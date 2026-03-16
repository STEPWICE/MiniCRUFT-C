using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MiniCRUFT.Core;
using MiniCRUFT.UI;
using MiniCRUFT.World;
using Veldrid;
using Veldrid.SPIRV;
using System.Text;

namespace MiniCRUFT.Renderer;

public sealed class WorldRenderer : IDisposable
{
    private readonly RenderDevice _device;
    private readonly TextureAtlas _atlas;
    private readonly ChunkMeshBuilder _meshBuilder;
    private readonly Dictionary<ChunkCoord, ChunkMesh> _meshes = new();
    private readonly BlockingCollection<MeshJob> _meshQueue = new();
    private readonly BlockingCollection<MeshJob> _meshQueueHigh = new();
    private readonly ConcurrentDictionary<ChunkCoord, int> _pendingMeshes = new();
    private readonly ConcurrentDictionary<ChunkCoord, byte> _ignoreResults = new();
    private readonly ConcurrentQueue<(ChunkCoord coord, MeshData data)> _meshResults = new();
    private readonly List<Task> _meshWorkers = new();
    private readonly CancellationTokenSource _cts = new();

    private readonly DeviceBuffer _cameraBuffer;
    private readonly ResourceLayout _resourceLayout;
    private readonly ResourceSet _resourceSet;
    private readonly Pipeline _solidPipeline;
    private readonly Pipeline _cutoutPipeline;
    private readonly Pipeline _transparentPipeline;

    private readonly UiRenderer _uiRenderer;
    private readonly SkyRenderer _skyRenderer;
    private readonly CloudRenderer _cloudRenderer;
    private readonly LodTerrainRenderer _lodRenderer;
    private readonly bool _useRowMajorMatrices;
    private bool _disableFrustum;
    private readonly RenderConfig _renderConfig;
    private readonly AtmosphereConfig _atmosphereConfig;

    public WorldRenderer(RenderDevice device, AssetStore assets, RenderConfig renderConfig, AtmosphereConfig atmosphereConfig, UiConfig uiConfig, WorldHeightSampler heightSampler, WorldGenSettings worldSettings)
    {
        _device = device;
        _renderConfig = renderConfig;
        _atmosphereConfig = atmosphereConfig;
        _useRowMajorMatrices = device.GraphicsDevice.BackendType == GraphicsBackend.Direct3D11;
        BlockRegistry.Initialize();
        var textureNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in BlockRegistry.All.Values)
        {
            textureNames.Add(def.TextureTop);
            textureNames.Add(def.TextureBottom);
            textureNames.Add(def.TextureSide);
        }
        _atlas = TextureAtlas.Build(device.GraphicsDevice, assets, renderConfig, textureNames);
        _meshBuilder = new ChunkMeshBuilder(_atlas, renderConfig, atmosphereConfig);

        uint cameraSize = (uint)Marshal.SizeOf<CameraUniform>();
        _cameraBuffer = device.GraphicsDevice.ResourceFactory.CreateBuffer(new BufferDescription(cameraSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        _resourceLayout = device.GraphicsDevice.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("CameraBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
            new ResourceLayoutElementDescription("AtlasTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("AtlasSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        _resourceSet = device.GraphicsDevice.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _resourceLayout,
            _cameraBuffer,
            _atlas.Texture,
            _atlas.Sampler));

        var shaderSet = CreateShaders(device.GraphicsDevice);
        _solidPipeline = CreatePipeline(device.GraphicsDevice, shaderSet, BlendStateDescription.SingleDisabled, depthWrite: true, cullMode: FaceCullMode.Back);
        _cutoutPipeline = CreatePipeline(device.GraphicsDevice, shaderSet, BlendStateDescription.SingleDisabled, depthWrite: true, cullMode: FaceCullMode.None);
        _transparentPipeline = CreatePipeline(device.GraphicsDevice, shaderSet, BlendStateDescription.SingleAlphaBlend, depthWrite: false, cullMode: FaceCullMode.Back);

        _uiRenderer = new UiRenderer(device.GraphicsDevice, assets, uiConfig);
        _skyRenderer = new SkyRenderer(device.GraphicsDevice, assets);
        _cloudRenderer = new CloudRenderer(device.GraphicsDevice, assets, renderConfig, atmosphereConfig);
        _lodRenderer = new LodTerrainRenderer(device.GraphicsDevice, heightSampler, worldSettings, renderConfig, atmosphereConfig);

        StartMeshWorkers(2);
    }

    private static ShaderSetDescription CreateShaders(GraphicsDevice device)
    {
        if (device.BackendType == GraphicsBackend.Direct3D11)
        {
            return CreateShadersHlsl(device);
        }

        const string vertexCode = @"#version 450
layout(set = 0, binding = 0) uniform CameraBuffer
{
    mat4 ViewProjection;
    vec4 FogColor;
    vec4 FogParams;
    vec4 CameraPos;
    vec4 Misc;
    vec4 HorizonParams;
    vec4 LightingParams;
    vec4 CutoutParams;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec2 LocalUV;
layout(location = 3) in vec2 AtlasMin;
layout(location = 4) in vec2 AtlasSize;
layout(location = 5) in float Light;
layout(location = 6) in vec4 Tint;
layout(location = 7) in float MaterialId;

layout(location = 0) out vec2 fsin_LocalUV;
layout(location = 1) out vec2 fsin_AtlasMin;
layout(location = 2) out vec2 fsin_AtlasSize;
layout(location = 3) out float fsin_Light;
layout(location = 4) out vec4 fsin_Tint;
layout(location = 5) out vec3 fsin_WorldPos;
layout(location = 6) out float fsin_MaterialId;

void main()
{
    fsin_LocalUV = LocalUV;
    fsin_AtlasMin = AtlasMin;
    fsin_AtlasSize = AtlasSize;
    fsin_Light = Light;
    fsin_Tint = Tint;
    fsin_WorldPos = Position;
    fsin_MaterialId = MaterialId;
    gl_Position = ViewProjection * vec4(Position, 1.0);
}
";

        const string fragmentCode = @"#version 450
layout(set = 0, binding = 1) uniform texture2D AtlasTexture;
layout(set = 0, binding = 2) uniform sampler AtlasSampler;

layout(set = 0, binding = 0) uniform CameraBuffer
{
    mat4 ViewProjection;
    vec4 FogColor;
    vec4 FogParams;
    vec4 CameraPos;
    vec4 Misc;
    vec4 HorizonParams;
    vec4 LightingParams;
    vec4 CutoutParams;
};

layout(location = 0) in vec2 fsin_LocalUV;
layout(location = 1) in vec2 fsin_AtlasMin;
layout(location = 2) in vec2 fsin_AtlasSize;
layout(location = 3) in float fsin_Light;
layout(location = 4) in vec4 fsin_Tint;
layout(location = 5) in vec3 fsin_WorldPos;
layout(location = 6) in float fsin_MaterialId;

layout(location = 0) out vec4 fsout_Color;

float ComputeFogFactor(float dist)
{
    float start = FogParams.x;
    float end = FogParams.y;
    float mode = FogParams.w;
    float denom = max(end - start, 0.001);
    if (mode < 0.5)
    {
        return clamp((end - dist) / denom, 0.0, 1.0);
    }
    float density = 1.0 / denom;
    return clamp(exp(-dist * density), 0.0, 1.0);
}

float ApplyHorizon(float fogFactor, vec3 worldPos, vec3 camPos, float strength, float power, float mask)
{
    vec3 dir = normalize(worldPos - camPos);
    float horiz = clamp(1.0 - abs(dir.y), 0.0, 1.0);
    horiz = pow(horiz, max(power, 0.01));
    float scale = clamp(1.0 - strength * horiz * mask, 0.0, 1.0);
    return fogFactor * scale;
}

vec3 LinearToSrgb(vec3 c)
{
    vec3 lo = 12.92 * c;
    vec3 hi = 1.055 * pow(c, vec3(1.0 / 2.4)) - 0.055;
    vec3 useHi = step(vec3(0.0031308), c);
    return mix(lo, hi, useHi);
}

float Bayer4x4(vec2 pos)
{
    vec2 p = mod(pos, 4.0);
    if (p.x < 0.0) p.x += 4.0;
    if (p.y < 0.0) p.y += 4.0;
    int x = int(p.x);
    int y = int(p.y);
    int index = x + y * 4;
    const float bayer[16] = float[16](
        0.0, 8.0, 2.0, 10.0,
        12.0, 4.0, 14.0, 6.0,
        3.0, 11.0, 1.0, 9.0,
        15.0, 7.0, 13.0, 5.0);
    return (bayer[index] + 0.5) / 16.0;
}

void main()
{
    const float WaterDepthScale = 0.6;
    const float WaterDeepFactor = 0.35;
    const float GlassDarken = 0.7;
    const float GlassFogMix = 0.1;
    float scroll = 0.0;
    if (fsin_Tint.a < 0.999)
    {
        scroll = fract(FogParams.z * Misc.y);
    }
    vec2 tiled = fract(fsin_LocalUV + vec2(scroll, scroll));
    tiled.y = 1.0 - tiled.y;
    tiled = clamp(tiled, vec2(0.0001), vec2(0.9999));
    vec2 uv = fsin_AtlasMin + tiled * fsin_AtlasSize;

    vec4 tex = texture(sampler2D(AtlasTexture, AtlasSampler), uv);
    float sun = clamp(CameraPos.w, 0.05, 1.0);
    float light = clamp(fsin_Light * mix(LightingParams.y, 1.0, sun), LightingParams.x, 1.0);
    vec3 tint = fsin_Tint.rgb;
    vec3 rgb = tex.rgb * light * tint;
    float dist = length(fsin_WorldPos - CameraPos.xyz);
    float horizonMask = smoothstep(FogParams.x, FogParams.y, dist);
    bool isFoliage = fsin_MaterialId > 0.5 && fsin_MaterialId < 1.5;
    bool isWater = fsin_MaterialId > 1.5 && fsin_MaterialId < 2.5;
    float alpha = tex.a * fsin_Tint.a;
    if (fsin_MaterialId < 0.5)
    {
        alpha = 1.0;
    }
    float cutout = isFoliage ? Misc.w : Misc.x;
    if (isFoliage && CutoutParams.x > 0.0)
    {
        float bayer = Bayer4x4(fsin_WorldPos.xz / max(CutoutParams.y, 0.001));
        float ditherStrength = CutoutParams.x * horizonMask;
        cutout += (bayer - 0.5) * ditherStrength;
    }
    if (alpha < cutout)
    {
        discard;
    }

    float fogFactor = ComputeFogFactor(dist);
    fogFactor = ApplyHorizon(fogFactor, fsin_WorldPos, CameraPos.xyz, HorizonParams.x, HorizonParams.y, horizonMask);

    if (isWater)
    {
        float depth = clamp(dist / max(FogParams.y * WaterDepthScale, 1.0), 0.0, 1.0);
        vec3 deep = tint * WaterDeepFactor;
        rgb = mix(rgb, deep, depth);
    }
    else if (fsin_Tint.a < 0.999)
    {
        float depth = clamp((dist - FogParams.x) / max(FogParams.y - FogParams.x, 0.001), 0.0, 1.0);
        rgb = mix(rgb, rgb * GlassDarken + FogColor.rgb * GlassFogMix, depth);
    }

    rgb = mix(FogColor.rgb, rgb, fogFactor);
    rgb = LinearToSrgb(rgb);
    fsout_Color = vec4(rgb, alpha);
}
";

        var factory = device.ResourceFactory;
        var shaders = factory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));

        return new ShaderSetDescription(new[] { VoxelVertex.Layout }, shaders);
    }

    private static ShaderSetDescription CreateShadersHlsl(GraphicsDevice device)
    {
        const string vertexCode = @"
cbuffer CameraBuffer : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 FogColor;
    float4 FogParams;
    float4 CameraPos;
    float4 Misc;
    float4 HorizonParams;
    float4 LightingParams;
    float4 CutoutParams;
};

struct VSInput
{
    float3 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 LocalUV  : TEXCOORD0;
    float2 AtlasMin : TEXCOORD1;
    float2 AtlasSize: TEXCOORD2;
    float  Light    : TEXCOORD3;
    float4 Tint     : TEXCOORD4;
    float  MaterialId : TEXCOORD5;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float2 LocalUV  : TEXCOORD0;
    float2 AtlasMin : TEXCOORD1;
    float2 AtlasSize: TEXCOORD2;
    float  Light    : TEXCOORD3;
    float4 Tint     : TEXCOORD4;
    float3 WorldPos : TEXCOORD5;
    float  MaterialId : TEXCOORD6;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.LocalUV = input.LocalUV;
    output.AtlasMin = input.AtlasMin;
    output.AtlasSize = input.AtlasSize;
    output.Light = input.Light;
    output.Tint = input.Tint;
    output.WorldPos = input.Position;
    output.MaterialId = input.MaterialId;
    output.Position = mul(float4(input.Position, 1.0), ViewProjection);
    return output;
}";

        const string fragmentCode = @"
Texture2D AtlasTexture : register(t0);
SamplerState AtlasSampler : register(s0);

cbuffer CameraBuffer : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 FogColor;
    float4 FogParams;
    float4 CameraPos;
    float4 Misc;
    float4 HorizonParams;
    float4 LightingParams;
    float4 CutoutParams;
};

struct PSInput
{
    float4 Position : SV_Position;
    float2 LocalUV  : TEXCOORD0;
    float2 AtlasMin : TEXCOORD1;
    float2 AtlasSize: TEXCOORD2;
    float  Light    : TEXCOORD3;
    float4 Tint     : TEXCOORD4;
    float3 WorldPos : TEXCOORD5;
    float  MaterialId : TEXCOORD6;
};

float ComputeFogFactor(float dist)
{
    float start = FogParams.x;
    float end = FogParams.y;
    float mode = FogParams.w;
    float denom = max(end - start, 0.001);
    if (mode < 0.5)
    {
        return saturate((end - dist) / denom);
    }
    float density = 1.0 / denom;
    return saturate(exp(-dist * density));
}

float ApplyHorizon(float fogFactor, float3 worldPos, float3 camPos, float strength, float power, float mask)
{
    float3 dir = normalize(worldPos - camPos);
    float horiz = saturate(1.0 - abs(dir.y));
    horiz = pow(horiz, max(power, 0.01));
    float scale = saturate(1.0 - strength * horiz * mask);
    return fogFactor * scale;
}

float3 LinearToSrgb(float3 c)
{
    float3 lo = 12.92 * c;
    float3 hi = 1.055 * pow(c, 1.0 / 2.4) - 0.055;
    float3 useHi = step(0.0031308, c);
    return lerp(lo, hi, useHi);
}

float Bayer4x4(float2 pos)
{
    float2 p = fmod(pos, 4.0);
    if (p.x < 0.0) p.x += 4.0;
    if (p.y < 0.0) p.y += 4.0;
    int x = (int)p.x;
    int y = (int)p.y;
    int index = x + y * 4;
    static const float bayer[16] = {
        0.0, 8.0, 2.0, 10.0,
        12.0, 4.0, 14.0, 6.0,
        3.0, 11.0, 1.0, 9.0,
        15.0, 7.0, 13.0, 5.0
    };
    return (bayer[index] + 0.5) / 16.0;
}

float4 main(PSInput input) : SV_Target
{
    const float WaterDepthScale = 0.6;
    const float WaterDeepFactor = 0.35;
    const float GlassDarken = 0.7;
    const float GlassFogMix = 0.1;
    float scroll = 0.0;
    if (input.Tint.a < 0.999)
    {
        scroll = frac(FogParams.z * Misc.y);
    }
    float2 tiled = frac(input.LocalUV + float2(scroll, scroll));
    tiled.y = 1.0 - tiled.y;
    tiled = clamp(tiled, 0.0001, 0.9999);
    float2 uv = input.AtlasMin + tiled * input.AtlasSize;

    float4 tex = AtlasTexture.Sample(AtlasSampler, uv);
    float sun = clamp(CameraPos.w, 0.05, 1.0);
    float light = clamp(input.Light * lerp(LightingParams.y, 1.0, sun), LightingParams.x, 1.0);
    float3 tint = input.Tint.rgb;
    float3 rgb = tex.rgb * light * tint;
    float dist = length(input.WorldPos - CameraPos.xyz);
    float horizonMask = smoothstep(FogParams.x, FogParams.y, dist);
    bool isFoliage = input.MaterialId > 0.5 && input.MaterialId < 1.5;
    bool isWater = input.MaterialId > 1.5 && input.MaterialId < 2.5;
    float alpha = tex.a * input.Tint.a;
    if (input.MaterialId < 0.5)
    {
        alpha = 1.0;
    }
    float cutout = isFoliage ? Misc.w : Misc.x;
    if (isFoliage && CutoutParams.x > 0.0)
    {
        float bayer = Bayer4x4(input.WorldPos.xz / max(CutoutParams.y, 0.001));
        float ditherStrength = CutoutParams.x * horizonMask;
        cutout += (bayer - 0.5) * ditherStrength;
    }
    if (alpha < cutout)
    {
        discard;
    }

    float fogFactor = ComputeFogFactor(dist);
    fogFactor = ApplyHorizon(fogFactor, input.WorldPos, CameraPos.xyz, HorizonParams.x, HorizonParams.y, horizonMask);

    if (isWater)
    {
        float depth = saturate(dist / max(FogParams.y * WaterDepthScale, 1.0));
        float3 deep = tint * WaterDeepFactor;
        rgb = lerp(rgb, deep, depth);
    }
    else if (input.Tint.a < 0.999)
    {
        float depth = saturate((dist - FogParams.x) / max(FogParams.y - FogParams.x, 0.001));
        rgb = lerp(rgb, rgb * GlassDarken + FogColor.rgb * GlassFogMix, depth);
    }

    rgb = lerp(FogColor.rgb, rgb, fogFactor);
    rgb = LinearToSrgb(rgb);
    return float4(rgb, alpha);
}";

        var factory = device.ResourceFactory;
        var vertex = factory.CreateShader(new ShaderDescription(
            ShaderStages.Vertex,
            Encoding.UTF8.GetBytes(vertexCode),
            "main"));
        var fragment = factory.CreateShader(new ShaderDescription(
            ShaderStages.Fragment,
            Encoding.UTF8.GetBytes(fragmentCode),
            "main"));

        return new ShaderSetDescription(new[] { VoxelVertex.Layout }, new[] { vertex, fragment });
    }

    private Pipeline CreatePipeline(GraphicsDevice device, ShaderSetDescription shaderSet, BlendStateDescription blend, bool depthWrite, FaceCullMode cullMode)
    {
        var depth = new DepthStencilStateDescription(
            depthTestEnabled: true,
            depthWriteEnabled: depthWrite,
            comparisonKind: ComparisonKind.LessEqual);

        var raster = new RasterizerStateDescription(
            cullMode: cullMode,
            fillMode: PolygonFillMode.Solid,
            frontFace: FrontFace.CounterClockwise,
            depthClipEnabled: true,
            scissorTestEnabled: false);

        var pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = blend,
            DepthStencilState = depth,
            RasterizerState = raster,
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { _resourceLayout },
            ShaderSet = shaderSet,
            Outputs = device.MainSwapchain.Framebuffer.OutputDescription
        };

        return device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);
    }

    public void EnqueueChunk(Chunk chunk, ChunkNeighborhood neighbors, bool highPriority = false)
    {
        var coord = new ChunkCoord(chunk.ChunkX, chunk.ChunkZ);
        _ignoreResults.TryRemove(coord, out _);
        int priority = highPriority ? 1 : 0;
        if (_pendingMeshes.TryGetValue(coord, out var existing))
        {
            if (priority <= existing)
            {
                return;
            }
            _pendingMeshes[coord] = priority;
        }
        else
        {
            _pendingMeshes[coord] = priority;
        }

        var job = new MeshJob(chunk, neighbors, priority);
        if (priority > 0)
        {
            _meshQueueHigh.Add(job);
        }
        else
        {
            _meshQueue.Add(job);
        }
    }

    public void RemoveChunkMesh(ChunkCoord coord)
    {
        _ignoreResults[coord] = 1;
        _pendingMeshes.TryRemove(coord, out _);
        if (_meshes.TryGetValue(coord, out var mesh))
        {
            mesh.Dispose();
            _meshes.Remove(coord);
        }
    }

    private void StartMeshWorkers(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _meshWorkers.Add(Task.Run(() =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    MeshJob job;
                    try
                    {
                        if (_meshQueueHigh.TryTake(out job))
                        {
                        }
                        else if (!_meshQueue.TryTake(out job, 10, _cts.Token))
                        {
                            continue;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                    if (!_pendingMeshes.TryGetValue(job.Coord, out var pending))
                    {
                        continue;
                    }

                    if (job.Priority < pending)
                    {
                        continue;
                    }

                    if (!_pendingMeshes.TryRemove(job.Coord, out _))
                    {
                        continue;
                    }

                    try
                    {
                        lock (job.Chunk.SyncRoot)
                        {
                            if (job.Chunk.LightingDirty)
                            {
                                WorldLighting.RecalculateChunkLighting(job.Chunk, job.Neighbors.North, job.Neighbors.South, job.Neighbors.East, job.Neighbors.West);
                            }
                            var data = _meshBuilder.Build(job.Neighbors);
                            _meshResults.Enqueue((job.Coord, data));
                            job.Chunk.ClearDirty();
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Mesh build failed for chunk {job.Chunk.ChunkX},{job.Chunk.ChunkZ}: {ex}");
                    }
                }
            }));
        }
    }

    public void UpdateMeshes()
    {
        int budget = Math.Max(1, _renderConfig.MaxMeshUploadsPerFrame);
        int processed = 0;
        while (processed < budget && _meshResults.TryDequeue(out var result))
        {
            if (_ignoreResults.TryRemove(result.coord, out _))
            {
                processed++;
                continue;
            }

            if (!_meshes.TryGetValue(result.coord, out var mesh))
            {
                mesh = new ChunkMesh();
                _meshes[result.coord] = mesh;
            }

            mesh.Update(_device.GraphicsDevice, result.data);
            processed++;
        }

        _lodRenderer.UpdateMeshes();
    }

    public void Draw(Camera camera, HudState hud, AtmosphereFrame atmosphere)
    {
        camera.UpdateMatrices();
        var viewProj = camera.View * camera.Projection;
        var matrix = _useRowMajorMatrices ? viewProj : Matrix4x4.Transpose(viewProj);
        var fogParams = new Vector4(_renderConfig.FogStart, _renderConfig.FogEnd, atmosphere.TimeSeconds, _renderConfig.LinearFog ? 0f : 1f);
        var cameraPos = new Vector4(camera.Position, atmosphere.SunIntensity);
        var misc = new Vector4(0.05f, _atmosphereConfig.WaterUvSpeed, _renderConfig.BiomeTintStrength, _renderConfig.Foliage.CutoutAlphaThreshold);
        var horizon = new Vector4(_renderConfig.Fog.HorizonBlendStrength, _renderConfig.Fog.HorizonBlendPower, 0f, 0f);
        var lighting = new Vector4(_renderConfig.MinLight, _renderConfig.SunLightMin, _renderConfig.Lod.ColorFogBlend, 0f);
        var cutoutParams = new Vector4(_renderConfig.Foliage.DitherStrength, _renderConfig.Foliage.DitherScale, 0f, 0f);
        var uniform = new CameraUniform(matrix, atmosphere.FogColor, fogParams, cameraPos, misc, horizon, lighting, cutoutParams);
        _device.GraphicsDevice.UpdateBuffer(_cameraBuffer, 0, ref uniform);
        _lodRenderer.Update(camera.Position);
        _lodRenderer.UpdateUniform(uniform);

        var frustum = Frustum.FromMatrix(viewProj);

        var commandList = _device.CommandList;
        commandList.Begin();
        commandList.SetFramebuffer(_device.GraphicsDevice.MainSwapchain.Framebuffer);
        var clear = ColorSpace.ToSrgb(atmosphere.SkyBottom);
        commandList.ClearColorTarget(0, new RgbaFloat(clear.X, clear.Y, clear.Z, 1f));
        commandList.ClearDepthStencil(1f);

        _skyRenderer.Draw(commandList, _device.Window.Width, _device.Window.Height, camera, atmosphere, _atmosphereConfig);
        _lodRenderer.Draw(commandList);

        commandList.SetPipeline(_solidPipeline);
        commandList.SetGraphicsResourceSet(0, _resourceSet);

        int solidCandidates = 0;
        int solidDrawn = 0;
        foreach (var (coord, mesh) in _meshes)
        {
            if (mesh.Solid.IndexCount == 0)
            {
                continue;
            }

            solidCandidates++;

            var aabb = new Aabb(
                new Vector3(coord.X * Chunk.SizeX, 0, coord.Z * Chunk.SizeZ),
                new Vector3(coord.X * Chunk.SizeX + Chunk.SizeX, Chunk.SizeY, coord.Z * Chunk.SizeZ + Chunk.SizeZ));

            if (!_disableFrustum && !frustum.Intersects(aabb))
            {
                continue;
            }

            commandList.SetVertexBuffer(0, mesh.Solid.VertexBuffer!);
            commandList.SetIndexBuffer(mesh.Solid.IndexBuffer!, IndexFormat.UInt32);
            commandList.DrawIndexed(mesh.Solid.IndexCount, 1, 0, 0, 0);
            solidDrawn++;
        }

        if (!_disableFrustum && solidCandidates > 0 && solidDrawn == 0)
        {
            _disableFrustum = true;
            Log.Warn("Frustum culling disabled due to zero visible chunks.");
        }

        var cutoutMisc = new Vector4(_renderConfig.CutoutAlphaThreshold, _atmosphereConfig.WaterUvSpeed, _renderConfig.BiomeTintStrength, _renderConfig.Foliage.CutoutAlphaThreshold);
        var cutoutUniform = new CameraUniform(matrix, atmosphere.FogColor, fogParams, cameraPos, cutoutMisc, horizon, lighting, cutoutParams);
        _device.GraphicsDevice.UpdateBuffer(_cameraBuffer, 0, ref cutoutUniform);

        commandList.SetPipeline(_cutoutPipeline);
        commandList.SetGraphicsResourceSet(0, _resourceSet);

        foreach (var (coord, mesh) in _meshes)
        {
            if (mesh.Cutout.IndexCount == 0)
            {
                continue;
            }

            var aabb = new Aabb(
                new Vector3(coord.X * Chunk.SizeX, 0, coord.Z * Chunk.SizeZ),
                new Vector3(coord.X * Chunk.SizeX + Chunk.SizeX, Chunk.SizeY, coord.Z * Chunk.SizeZ + Chunk.SizeZ));

            if (!_disableFrustum && !frustum.Intersects(aabb))
            {
                continue;
            }

            commandList.SetVertexBuffer(0, mesh.Cutout.VertexBuffer!);
            commandList.SetIndexBuffer(mesh.Cutout.IndexBuffer!, IndexFormat.UInt32);
            commandList.DrawIndexed(mesh.Cutout.IndexCount, 1, 0, 0, 0);
        }

        _cloudRenderer.Update(camera.Position);
        _cloudRenderer.Draw(commandList, camera, atmosphere);

        _device.GraphicsDevice.UpdateBuffer(_cameraBuffer, 0, ref uniform);

        commandList.SetPipeline(_transparentPipeline);
        commandList.SetGraphicsResourceSet(0, _resourceSet);

        var transparentList = new List<(ChunkCoord coord, ChunkMesh mesh, float distance)>();
        foreach (var (coord, mesh) in _meshes)
        {
            if (mesh.Transparent.IndexCount == 0)
            {
                continue;
            }

            var center = new Vector3(coord.X * Chunk.SizeX + Chunk.SizeX * 0.5f, Chunk.SizeY * 0.5f, coord.Z * Chunk.SizeZ + Chunk.SizeZ * 0.5f);
            float dist = Vector3.DistanceSquared(center, camera.Position);
            transparentList.Add((coord, mesh, dist));
        }

        transparentList.Sort((a, b) => b.distance.CompareTo(a.distance));

        foreach (var item in transparentList)
        {
            var coord = item.coord;
            var mesh = item.mesh;

            var aabb = new Aabb(
                new Vector3(coord.X * Chunk.SizeX, 0, coord.Z * Chunk.SizeZ),
                new Vector3(coord.X * Chunk.SizeX + Chunk.SizeX, Chunk.SizeY, coord.Z * Chunk.SizeZ + Chunk.SizeZ));

            if (!_disableFrustum && !frustum.Intersects(aabb))
            {
                continue;
            }

            commandList.SetVertexBuffer(0, mesh.Transparent.VertexBuffer!);
            commandList.SetIndexBuffer(mesh.Transparent.IndexBuffer!, IndexFormat.UInt32);
            commandList.DrawIndexed(mesh.Transparent.IndexCount, 1, 0, 0, 0);
        }

        _uiRenderer.Draw(commandList, _device.GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription, hud, _device.Window.Width, _device.Window.Height);

        commandList.End();
        _device.GraphicsDevice.SubmitCommands(commandList);
        _device.GraphicsDevice.SwapBuffers(_device.GraphicsDevice.MainSwapchain);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _meshQueue.CompleteAdding();
        _meshQueueHigh.CompleteAdding();
        try
        {
            Task.WaitAll(_meshWorkers.ToArray(), 2000);
        }
        catch
        {
        }

        foreach (var mesh in _meshes.Values)
        {
            mesh.Dispose();
        }

        _solidPipeline.Dispose();
        _cutoutPipeline.Dispose();
        _transparentPipeline.Dispose();
        _resourceSet.Dispose();
        _resourceLayout.Dispose();
        _cameraBuffer.Dispose();
        _atlas.Dispose();
        _uiRenderer.Dispose();
        _skyRenderer.Dispose();
        _cloudRenderer.Dispose();
        _lodRenderer.Dispose();
    }

    private readonly struct MeshJob
    {
        public Chunk Chunk { get; }
        public ChunkNeighborhood Neighbors { get; }
        public ChunkCoord Coord { get; }
        public int Priority { get; }

        public MeshJob(Chunk chunk, ChunkNeighborhood neighbors, int priority)
        {
            Chunk = chunk;
            Neighbors = neighbors;
            Coord = new ChunkCoord(chunk.ChunkX, chunk.ChunkZ);
            Priority = priority;
        }
    }
}

public readonly struct CameraUniform
{
    public readonly Matrix4x4 ViewProjection;
    public readonly Vector4 FogColor;
    public readonly Vector4 FogParams;
    public readonly Vector4 CameraPos;
    public readonly Vector4 Misc;
    public readonly Vector4 HorizonParams;
    public readonly Vector4 LightingParams;
    public readonly Vector4 CutoutParams;

    public CameraUniform(Matrix4x4 viewProjection, Vector4 fogColor, Vector4 fogParams, Vector4 cameraPos, Vector4 misc, Vector4 horizonParams, Vector4 lightingParams, Vector4 cutoutParams)
    {
        ViewProjection = viewProjection;
        FogColor = fogColor;
        FogParams = fogParams;
        CameraPos = cameraPos;
        Misc = misc;
        HorizonParams = horizonParams;
        LightingParams = lightingParams;
        CutoutParams = cutoutParams;
    }
}
