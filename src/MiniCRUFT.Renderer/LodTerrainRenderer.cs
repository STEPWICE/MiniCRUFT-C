using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using Veldrid;
using Veldrid.SPIRV;

namespace MiniCRUFT.Renderer;

public sealed class LodTerrainRenderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly LodConfig _config;
    private readonly AtmosphereConfig _atmosphere;
    private readonly WorldHeightSampler _sampler;
    private readonly int _seaLevel;
    private readonly WorldGenSettings _worldSettings;
    private readonly float _biomeTintStrength;
    private readonly float _cliffThreshold;
    private readonly Vector3 _sandColor;
    private readonly Vector3 _stoneColor;
    private readonly Vector3 _waterTintLinear;
    private readonly Vector3 _lightDir = Vector3.Normalize(new Vector3(0.4f, 1f, 0.3f));

    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    private uint _indexCount;

    private readonly DeviceBuffer _cameraBuffer;
    private readonly ResourceLayout _layout;
    private readonly ResourceSet _set;
    private readonly Pipeline _pipeline;

    private Task<LodMeshData>? _buildTask;
    private bool _hasPending;
    private Vector3 _pendingCenter;
    private Vector3 _currentCenter;

    public LodTerrainRenderer(GraphicsDevice device, WorldHeightSampler sampler, WorldGenSettings settings, RenderConfig renderConfig, AtmosphereConfig atmosphere)
    {
        _device = device;
        _sampler = sampler;
        _config = renderConfig.Lod;
        _atmosphere = atmosphere;
        _seaLevel = settings.SeaLevel;
        _worldSettings = settings;
        _biomeTintStrength = renderConfig.BiomeTintStrength;
        _cliffThreshold = settings.CliffSlopeThreshold * 1.35f;
        _sandColor = ColorSpace.ToLinear(renderConfig.Palette.SandTint.ToVector3());
        _stoneColor = ColorSpace.ToLinear(renderConfig.Palette.StoneTint.ToVector3());
        _waterTintLinear = ColorSpace.ToLinear(atmosphere.WaterTint.ToVector3());

        _vertexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(1024, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _indexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(1024, BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        uint cameraSize = (uint)Marshal.SizeOf<CameraUniform>();
        _cameraBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(cameraSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        _layout = device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("CameraBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));
        _set = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_layout, _cameraBuffer));

        var shaderSet = CreateShaders(device);
        _pipeline = device.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleDisabled,
            DepthStencilState = new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { _layout },
            ShaderSet = shaderSet,
            Outputs = device.MainSwapchain.Framebuffer.OutputDescription
        });
    }

    public void Update(Vector3 cameraPosition)
    {
        if (!_config.Enabled)
        {
            return;
        }

        var center = AlignCenter(cameraPosition);
        float threshold = _config.Step * _config.Step;
        if (Vector3.DistanceSquared(center, _currentCenter) < threshold)
        {
            return;
        }

        if (_buildTask == null)
        {
            _buildTask = Task.Run(() => BuildMesh(center));
        }
        else
        {
            _pendingCenter = center;
            _hasPending = true;
        }
    }

    public void UpdateUniform(CameraUniform uniform)
    {
        if (!_config.Enabled)
        {
            return;
        }

        _device.UpdateBuffer(_cameraBuffer, 0, ref uniform);
    }

    public void UpdateMeshes()
    {
        if (!_config.Enabled || _buildTask == null)
        {
            return;
        }

        if (!_buildTask.IsCompleted)
        {
            return;
        }

        if (_buildTask.IsFaulted)
        {
            Log.Warn($"LOD build failed: {_buildTask.Exception}");
        }
        else
        {
            Upload(_buildTask.Result);
            _currentCenter = _buildTask.Result.Center;
        }

        _buildTask = null;
        if (_hasPending)
        {
            var next = _pendingCenter;
            _hasPending = false;
            _buildTask = Task.Run(() => BuildMesh(next));
        }
    }

    public void Draw(CommandList commandList)
    {
        if (!_config.Enabled || _indexCount == 0)
        {
            return;
        }

        commandList.SetPipeline(_pipeline);
        commandList.SetGraphicsResourceSet(0, _set);
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);
        commandList.DrawIndexed(_indexCount, 1, 0, 0, 0);
    }

    private Vector3 AlignCenter(Vector3 position)
    {
        float step = Math.Max(1f, _config.Step);
        float x = MathF.Floor(position.X / step) * step;
        float z = MathF.Floor(position.Z / step) * step;
        return new Vector3(x, 0f, z);
    }

    private LodMeshData BuildMesh(Vector3 center)
    {
        float inner = Math.Max(0f, _config.StartDistance);
        float outer = Math.Max(inner + _config.Step, _config.EndDistance);
        float step = Math.Max(1f, _config.Step);

        int steps = (int)MathF.Ceiling((outer * 2f) / step);
        float startX = center.X - outer;
        float startZ = center.Z - outer;

        var vertices = new List<LodVertex>(steps * steps);
        var indices = new List<uint>(steps * steps * 6);

        for (int z = 0; z < steps; z++)
        {
            float z0 = startZ + z * step;
            float z1 = z0 + step;
            for (int x = 0; x < steps; x++)
            {
                float x0 = startX + x * step;
                float x1 = x0 + step;

                float d0 = Distance2D(x0, z0, center);
                float d1 = Distance2D(x1, z0, center);
                float d2 = Distance2D(x1, z1, center);
                float d3 = Distance2D(x0, z1, center);
                float maxDist = MathF.Max(MathF.Max(d0, d1), MathF.Max(d2, d3));

                if (maxDist < inner)
                {
                    continue;
                }
                if (d0 > outer && d1 > outer && d2 > outer && d3 > outer)
                {
                    continue;
                }

                var v0 = BuildVertex(x0, z0);
                var v1 = BuildVertex(x1, z0);
                var v2 = BuildVertex(x1, z1);
                var v3 = BuildVertex(x0, z1);

                uint start = (uint)vertices.Count;
                vertices.Add(v0);
                vertices.Add(v1);
                vertices.Add(v2);
                vertices.Add(v3);

                indices.Add(start);
                indices.Add(start + 1);
                indices.Add(start + 2);
                indices.Add(start);
                indices.Add(start + 2);
                indices.Add(start + 3);
            }
        }

        return new LodMeshData(center, vertices, indices);
    }

    private LodVertex BuildVertex(float x, float z)
    {
        int ix = (int)x;
        int iz = (int)z;
        int height = _sampler.GetSmoothedHeight(ix, iz);
        int heightX = _sampler.GetSmoothedHeight(ix + 1, iz);
        int heightZ = _sampler.GetSmoothedHeight(ix, iz + 1);
        int heightXm = _sampler.GetSmoothedHeight(ix - 1, iz);
        int heightZm = _sampler.GetSmoothedHeight(ix, iz - 1);
        float slope = MathF.Max(MathF.Abs(height - heightX), MathF.Abs(height - heightZ));

        float river = _sampler.GetRiverValue(ix, iz);
        var biome = _sampler.GetBiomeId(ix, iz, river);

        bool cliff = slope > _cliffThreshold;
        BlockId surface = BiomeRegistry.Get(biome).SurfaceBlock;
        if (height <= _seaLevel + _worldSettings.BeachSize && !cliff)
        {
            surface = BlockId.Sand;
        }
        if (cliff)
        {
            surface = BlockId.Stone;
        }

        float y = height;
        Vector3 color = surface switch
        {
            BlockId.Sand => _sandColor,
            BlockId.Stone => _stoneColor,
            _ =>
                Vector3.Lerp(
                    Vector3.One,
                    ColorSpace.ToLinear(BiomeRegistry.GetGrassColor(biome)),
                    _biomeTintStrength)
        };

        var normal = Vector3.Normalize(new Vector3(heightXm - heightX, 2f, heightZm - heightZ));
        float shade = Math.Clamp(Vector3.Dot(normal, _lightDir) * 0.35f + 0.75f, 0.75f, 1f);
        color *= shade;
        if (height < _seaLevel)
        {
            y = _seaLevel;
            float waterBlend = _worldSettings.StrictBetaMode ? 0.68f : 0.75f;
            color = Vector3.Lerp(color, _waterTintLinear, waterBlend);
        }

        return new LodVertex(new Vector3(x, y, z), new Vector4(color, 1f));
    }

    private static float Distance2D(float x, float z, Vector3 center)
    {
        float dx = x - center.X;
        float dz = z - center.Z;
        return MathF.Sqrt(dx * dx + dz * dz);
    }

    private void Upload(LodMeshData data)
    {
        if (data.Vertices.Count == 0 || data.Indices.Count == 0)
        {
            _indexCount = 0;
            return;
        }

        uint vertexSize = (uint)(data.Vertices.Count * Marshal.SizeOf<LodVertex>());
        if (vertexSize > _vertexBuffer.SizeInBytes)
        {
            _vertexBuffer.Dispose();
            _vertexBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(vertexSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        uint indexSize = (uint)(data.Indices.Count * sizeof(uint));
        if (indexSize > _indexBuffer.SizeInBytes)
        {
            _indexBuffer.Dispose();
            _indexBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(indexSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        }

        _device.UpdateBuffer(_vertexBuffer, 0, CollectionsMarshal.AsSpan(data.Vertices));
        _device.UpdateBuffer(_indexBuffer, 0, CollectionsMarshal.AsSpan(data.Indices));
        _indexCount = (uint)data.Indices.Count;
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
    vec4 FaceShadingParams;
    vec4 PaletteSand;
    vec4 PaletteStone;
    vec4 CutoutParams;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec4 Color;

layout(location = 0) out vec4 fsin_Color;
layout(location = 1) out vec3 fsin_WorldPos;

void main()
{
    fsin_Color = Color;
    fsin_WorldPos = Position;
    gl_Position = ViewProjection * vec4(Position, 1.0);
}
";

        const string fragmentCode = @"#version 450
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

layout(location = 0) in vec4 fsin_Color;
layout(location = 1) in vec3 fsin_WorldPos;
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

void main()
{
    float dist = length(fsin_WorldPos - CameraPos.xyz);
    float fogFactor = ComputeFogFactor(dist);
    float horizonMask = smoothstep(FogParams.x, FogParams.y, dist);
    fogFactor = ApplyHorizon(fogFactor, fsin_WorldPos, CameraPos.xyz, HorizonParams.x, HorizonParams.y, horizonMask);

    vec3 color = mix(fsin_Color.rgb, FogColor.rgb, LightingParams.z);
    vec3 rgb = mix(FogColor.rgb, color, fogFactor);
    rgb = LinearToSrgb(rgb);
    fsout_Color = vec4(rgb, 1.0);
}
";

        var factory = device.ResourceFactory;
        var shaders = factory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));

        return new ShaderSetDescription(new[] { LodVertex.Layout }, shaders);
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
    float4 FaceShadingParams;
    float4 PaletteSand;
    float4 PaletteStone;
    float4 CutoutParams;
};

struct VSInput
{
    float3 Position : POSITION0;
    float4 Color    : COLOR0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float4 Color    : COLOR0;
    float3 WorldPos : TEXCOORD0;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.Color = input.Color;
    output.WorldPos = input.Position;
    output.Position = mul(float4(input.Position, 1.0), ViewProjection);
    return output;
}";

        const string fragmentCode = @"
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
    float4 Color    : COLOR0;
    float3 WorldPos : TEXCOORD0;
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

float4 main(PSInput input) : SV_Target
{
    float dist = length(input.WorldPos - CameraPos.xyz);
    float fogFactor = ComputeFogFactor(dist);
    float horizonMask = smoothstep(FogParams.x, FogParams.y, dist);
    fogFactor = ApplyHorizon(fogFactor, input.WorldPos, CameraPos.xyz, HorizonParams.x, HorizonParams.y, horizonMask);

    float3 color = lerp(input.Color.rgb, FogColor.rgb, LightingParams.z);
    float3 rgb = lerp(FogColor.rgb, color, fogFactor);
    rgb = LinearToSrgb(rgb);
    return float4(rgb, 1.0);
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

        return new ShaderSetDescription(new[] { LodVertex.Layout }, new[] { vertex, fragment });
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        _set.Dispose();
        _layout.Dispose();
        _cameraBuffer.Dispose();
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }

    private sealed class LodMeshData
    {
        public Vector3 Center { get; }
        public List<LodVertex> Vertices { get; }
        public List<uint> Indices { get; }

        public LodMeshData(Vector3 center, List<LodVertex> vertices, List<uint> indices)
        {
            Center = center;
            Vertices = vertices;
            Indices = indices;
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct LodVertex
    {
        public Vector3 Position;
        public Vector4 Color;

        public LodVertex(Vector3 position, Vector4 color)
        {
            Position = position;
            Color = color;
        }

        public static readonly VertexLayoutDescription Layout = new(
            new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
            new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));
    }
}
