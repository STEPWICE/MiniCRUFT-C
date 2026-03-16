using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using MiniCRUFT.Core;
using Veldrid;
using Veldrid.SPIRV;

namespace MiniCRUFT.Renderer;

public sealed class CloudRenderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private readonly AtmosphereConfig _atmosphere;
    private readonly RenderConfig _renderConfig;
    private readonly SpriteTexture _cloudTexture;
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    private readonly DeviceBuffer _uniformBuffer;
    private readonly ResourceLayout _layout;
    private readonly ResourceSet _set;
    private readonly Pipeline _pipeline;

    private Vector3 _currentCenter;
    private bool _dirty = true;
    private uint _indexCount;
    private readonly bool _useRowMajorMatrices;

    public CloudRenderer(GraphicsDevice device, AssetStore assets, RenderConfig renderConfig, AtmosphereConfig atmosphere)
    {
        _device = device;
        _renderConfig = renderConfig;
        _atmosphere = atmosphere;
        _useRowMajorMatrices = device.BackendType == GraphicsBackend.Direct3D11;
        _cloudTexture = SpriteTexture.Load(device, assets, "minecraft/textures/environment/clouds.png", repeat: true);

        _vertexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(1024, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _indexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(1024, BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        uint uniformSize = (uint)Marshal.SizeOf<CloudUniform>();
        _uniformBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(uniformSize, BufferUsage.UniformBuffer | BufferUsage.Dynamic));

        _layout = device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("CloudUniform", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
            new ResourceLayoutElementDescription("CloudTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("CloudSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        _set = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_layout, _uniformBuffer, _cloudTexture.Texture, _cloudTexture.Sampler));

        var shaders = CreateShaders(device);
        _pipeline = device.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription(true, false, ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.CounterClockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { _layout },
            ShaderSet = shaders,
            Outputs = device.MainSwapchain.Framebuffer.OutputDescription
        });
    }

    public void Update(Vector3 cameraPosition)
    {
        var center = AlignCenter(cameraPosition);
        if (center != _currentCenter)
        {
            _currentCenter = center;
            _dirty = true;
        }
    }

    public void Draw(CommandList commandList, Camera camera, AtmosphereFrame atmosphere)
    {
        if (_dirty)
        {
            BuildMesh();
            _dirty = false;
        }

        if (_indexCount == 0)
        {
            return;
        }

        UpdateUniform(camera, atmosphere);

        commandList.SetPipeline(_pipeline);
        commandList.SetGraphicsResourceSet(0, _set);
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);
        commandList.DrawIndexed(_indexCount, 1, 0, 0, 0);
    }

    private void BuildMesh()
    {
        float radius = Math.Max(32f, _atmosphere.CloudRadius);
        float cell = Math.Max(4f, _atmosphere.CloudCellSize);
        int steps = (int)MathF.Ceiling((radius * 2f) / cell);

        var vertices = new List<CloudVertex>(steps * steps * 4);
        var indices = new List<uint>(steps * steps * 6);

        float startX = _currentCenter.X - radius;
        float startZ = _currentCenter.Z - radius;
        float y = _atmosphere.CloudHeight;

        for (int z = 0; z < steps; z++)
        {
            float z0 = startZ + z * cell;
            float z1 = z0 + cell;
            for (int x = 0; x < steps; x++)
            {
                float x0 = startX + x * cell;
                float x1 = x0 + cell;

                uint start = (uint)vertices.Count;
                vertices.Add(new CloudVertex(new Vector3(x0, y, z0)));
                vertices.Add(new CloudVertex(new Vector3(x1, y, z0)));
                vertices.Add(new CloudVertex(new Vector3(x1, y, z1)));
                vertices.Add(new CloudVertex(new Vector3(x0, y, z1)));

                indices.Add(start);
                indices.Add(start + 1);
                indices.Add(start + 2);
                indices.Add(start);
                indices.Add(start + 2);
                indices.Add(start + 3);
            }
        }

        Upload(vertices, indices);
    }

    private void Upload(List<CloudVertex> vertices, List<uint> indices)
    {
        if (vertices.Count == 0 || indices.Count == 0)
        {
            _indexCount = 0;
            return;
        }

        uint vertexSize = (uint)(vertices.Count * Marshal.SizeOf<CloudVertex>());
        if (vertexSize > _vertexBuffer.SizeInBytes)
        {
            _vertexBuffer.Dispose();
            _vertexBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(vertexSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        uint indexSize = (uint)(indices.Count * sizeof(uint));
        if (indexSize > _indexBuffer.SizeInBytes)
        {
            _indexBuffer.Dispose();
            _indexBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(indexSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        }

        _device.UpdateBuffer(_vertexBuffer, 0, vertices.ToArray());
        _device.UpdateBuffer(_indexBuffer, 0, indices.ToArray());
        _indexCount = (uint)indices.Count;
    }

    private void UpdateUniform(Camera camera, AtmosphereFrame atmosphere)
    {
        var viewProj = camera.View * camera.Projection;
        var matrix = _useRowMajorMatrices ? viewProj : Matrix4x4.Transpose(viewProj);
        var fogParams = new Vector4(_renderConfig.FogStart, _renderConfig.FogEnd, atmosphere.TimeSeconds, _renderConfig.LinearFog ? 0f : 1f);
        var cameraPos = new Vector4(camera.Position, atmosphere.SunIntensity);
        float uvScale = _atmosphere.CloudTiling / MathF.Max(1f, _cloudTexture.Size.X);
        var cloudParams = new Vector4(uvScale, atmosphere.CloudOffset, _atmosphere.CloudOpacity, _atmosphere.CloudRadius);
        var horizonParams = new Vector4(_renderConfig.Fog.HorizonBlendStrength, _renderConfig.Fog.HorizonBlendPower, 0f, 0f);
        var uniform = new CloudUniform(matrix, atmosphere.FogColor, fogParams, cameraPos, cloudParams, horizonParams);
        _device.UpdateBuffer(_uniformBuffer, 0, ref uniform);
    }

    private Vector3 AlignCenter(Vector3 position)
    {
        float step = Math.Max(4f, _atmosphere.CloudCellSize);
        float x = MathF.Floor(position.X / step) * step;
        float z = MathF.Floor(position.Z / step) * step;
        return new Vector3(x, 0f, z);
    }

    private static ShaderSetDescription CreateShaders(GraphicsDevice device)
    {
        if (device.BackendType == GraphicsBackend.Direct3D11)
        {
            return CreateShadersHlsl(device);
        }

        const string vertexCode = @"#version 450
layout(set = 0, binding = 0) uniform CloudUniform
{
    mat4 ViewProjection;
    vec4 FogColor;
    vec4 FogParams;
    vec4 CameraPos;
    vec4 CloudParams;
    vec4 HorizonParams;
};

layout(location = 0) in vec3 Position;

layout(location = 0) out vec3 fsin_WorldPos;

void main()
{
    fsin_WorldPos = Position;
    gl_Position = ViewProjection * vec4(Position, 1.0);
}
";

        const string fragmentCode = @"#version 450
layout(set = 0, binding = 1) uniform texture2D CloudTexture;
layout(set = 0, binding = 2) uniform sampler CloudSampler;

layout(set = 0, binding = 0) uniform CloudUniform
{
    mat4 ViewProjection;
    vec4 FogColor;
    vec4 FogParams;
    vec4 CameraPos;
    vec4 CloudParams;
    vec4 HorizonParams;
};

layout(location = 0) in vec3 fsin_WorldPos;
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
    const float OffsetScale = 0.35;
    const float EdgeFade = 0.15;
    vec2 uv = fsin_WorldPos.xz * CloudParams.x + vec2(CloudParams.y, CloudParams.y * OffsetScale);
    vec4 tex = texture(sampler2D(CloudTexture, CloudSampler), uv);
    float sun = clamp(CameraPos.w, 0.1, 1.0);

    float dist = length(fsin_WorldPos.xz - CameraPos.xz);
    float fade = 1.0 - smoothstep(CloudParams.w * (1.0 - EdgeFade), CloudParams.w, dist);
    float alpha = tex.a * CloudParams.z * fade;
    if (alpha <= 0.01)
    {
        discard;
    }

    vec3 rgb = tex.rgb * mix(0.7, 1.0, sun);
    float fogFactor = ComputeFogFactor(length(fsin_WorldPos - CameraPos.xyz));
    float horizonMask = smoothstep(FogParams.x, FogParams.y, dist);
    fogFactor = ApplyHorizon(fogFactor, fsin_WorldPos, CameraPos.xyz, HorizonParams.x, HorizonParams.y, horizonMask);
    rgb = mix(FogColor.rgb, rgb, fogFactor);
    rgb = LinearToSrgb(rgb);
    fsout_Color = vec4(rgb, alpha);
}
";

        var factory = device.ResourceFactory;
        var shaders = factory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));

        return new ShaderSetDescription(new[] { CloudVertex.Layout }, shaders);
    }

    private static ShaderSetDescription CreateShadersHlsl(GraphicsDevice device)
    {
        const string vertexCode = @"
cbuffer CloudUniform : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 FogColor;
    float4 FogParams;
    float4 CameraPos;
    float4 CloudParams;
    float4 HorizonParams;
};

struct VSInput
{
    float3 Position : POSITION0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float3 WorldPos : TEXCOORD0;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.WorldPos = input.Position;
    output.Position = mul(float4(input.Position, 1.0), ViewProjection);
    return output;
}";

        const string fragmentCode = @"
Texture2D CloudTexture : register(t0);
SamplerState CloudSampler : register(s0);

cbuffer CloudUniform : register(b0)
{
    row_major float4x4 ViewProjection;
    float4 FogColor;
    float4 FogParams;
    float4 CameraPos;
    float4 CloudParams;
    float4 HorizonParams;
};

struct PSInput
{
    float4 Position : SV_Position;
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
    const float OffsetScale = 0.35;
    const float EdgeFade = 0.15;
    float2 uv = input.WorldPos.xz * CloudParams.x + float2(CloudParams.y, CloudParams.y * OffsetScale);
    float4 tex = CloudTexture.Sample(CloudSampler, uv);
    float sun = clamp(CameraPos.w, 0.1, 1.0);

    float distFlat = length(input.WorldPos.xz - CameraPos.xz);
    float fade = 1.0 - smoothstep(CloudParams.w * (1.0 - EdgeFade), CloudParams.w, distFlat);
    float alpha = tex.a * CloudParams.z * fade;
    if (alpha <= 0.01)
    {
        discard;
    }

    float3 rgb = tex.rgb * lerp(0.7, 1.0, sun);
    float fogFactor = ComputeFogFactor(length(input.WorldPos - CameraPos.xyz));
    float horizonMask = smoothstep(FogParams.x, FogParams.y, distFlat);
    fogFactor = ApplyHorizon(fogFactor, input.WorldPos, CameraPos.xyz, HorizonParams.x, HorizonParams.y, horizonMask);
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

        return new ShaderSetDescription(new[] { CloudVertex.Layout }, new[] { vertex, fragment });
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        _set.Dispose();
        _layout.Dispose();
        _uniformBuffer.Dispose();
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _cloudTexture.Dispose();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct CloudVertex
    {
        public Vector3 Position;

        public CloudVertex(Vector3 position)
        {
            Position = position;
        }

        public static readonly VertexLayoutDescription Layout = new(
            new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3));
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct CloudUniform
    {
        public readonly Matrix4x4 ViewProjection;
        public readonly Vector4 FogColor;
        public readonly Vector4 FogParams;
        public readonly Vector4 CameraPos;
        public readonly Vector4 CloudParams;
        public readonly Vector4 HorizonParams;

        public CloudUniform(Matrix4x4 viewProjection, Vector4 fogColor, Vector4 fogParams, Vector4 cameraPos, Vector4 cloudParams, Vector4 horizonParams)
        {
            ViewProjection = viewProjection;
            FogColor = fogColor;
            FogParams = fogParams;
            CameraPos = cameraPos;
            CloudParams = cloudParams;
            HorizonParams = horizonParams;
        }
    }
}
