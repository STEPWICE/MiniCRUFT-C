using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace MiniCRUFT.Renderer;

public sealed class SpriteBatch : IDisposable
{
    private readonly DeviceBuffer _vertexBuffer;
    private readonly DeviceBuffer _indexBuffer;
    private readonly Pipeline _pipeline;
    private readonly ResourceLayout _layout;
    private readonly ResourceSet _set;

    private readonly List<SpriteVertex> _vertices = new();
    private readonly List<ushort> _indices = new();

    private int _width;
    private int _height;

    public SpriteBatch(GraphicsDevice device, Texture texture, Sampler sampler)
    {
        _vertexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(1024 * 1024, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _indexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(256 * 1024, BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        _layout = device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("SpriteTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("SpriteSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        _set = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_layout, texture, sampler));

        var shaderSet = CreateShaders(device);
        var pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = DepthStencilStateDescription.Disabled,
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { _layout },
            ShaderSet = shaderSet,
            Outputs = device.MainSwapchain.Framebuffer.OutputDescription
        };

        _pipeline = device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);
    }

    public void Begin(int width, int height)
    {
        _width = width;
        _height = height;
        _vertices.Clear();
        _indices.Clear();
    }

    public void Draw(SpriteRegion region, Vector2 position, Vector2 size, Vector4 color)
    {
        var p0 = ToNdc(position.X, position.Y);
        var p1 = ToNdc(position.X + size.X, position.Y);
        var p2 = ToNdc(position.X + size.X, position.Y + size.Y);
        var p3 = ToNdc(position.X, position.Y + size.Y);

        var uv0 = region.Min;
        var uv1 = new Vector2(region.Max.X, region.Min.Y);
        var uv2 = region.Max;
        var uv3 = new Vector2(region.Min.X, region.Max.Y);

        ushort start = (ushort)_vertices.Count;
        _vertices.Add(new SpriteVertex(p0, uv0, color));
        _vertices.Add(new SpriteVertex(p1, uv1, color));
        _vertices.Add(new SpriteVertex(p2, uv2, color));
        _vertices.Add(new SpriteVertex(p3, uv3, color));

        _indices.Add(start);
        _indices.Add((ushort)(start + 1));
        _indices.Add((ushort)(start + 2));
        _indices.Add(start);
        _indices.Add((ushort)(start + 2));
        _indices.Add((ushort)(start + 3));
    }

    public void Draw(SpriteRegion region, Vector2 position, Vector2 size, Vector4 color, Vector2 uvOffset, Vector2 uvScale)
    {
        var p0 = ToNdc(position.X, position.Y);
        var p1 = ToNdc(position.X + size.X, position.Y);
        var p2 = ToNdc(position.X + size.X, position.Y + size.Y);
        var p3 = ToNdc(position.X, position.Y + size.Y);

        var baseMin = region.Min;
        var scale = uvScale * (region.Max - region.Min);
        var uv0 = baseMin + uvOffset;
        var uv1 = baseMin + new Vector2(uvOffset.X + scale.X, uvOffset.Y);
        var uv2 = baseMin + new Vector2(uvOffset.X + scale.X, uvOffset.Y + scale.Y);
        var uv3 = baseMin + new Vector2(uvOffset.X, uvOffset.Y + scale.Y);

        ushort start = (ushort)_vertices.Count;
        _vertices.Add(new SpriteVertex(p0, uv0, color));
        _vertices.Add(new SpriteVertex(p1, uv1, color));
        _vertices.Add(new SpriteVertex(p2, uv2, color));
        _vertices.Add(new SpriteVertex(p3, uv3, color));

        _indices.Add(start);
        _indices.Add((ushort)(start + 1));
        _indices.Add((ushort)(start + 2));
        _indices.Add(start);
        _indices.Add((ushort)(start + 2));
        _indices.Add((ushort)(start + 3));
    }

    public void Flush(CommandList commandList)
    {
        if (_indices.Count == 0)
        {
            return;
        }

        commandList.SetPipeline(_pipeline);
        commandList.SetGraphicsResourceSet(0, _set);
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        commandList.UpdateBuffer(_vertexBuffer, 0, CollectionsMarshal.AsSpan(_vertices));
        commandList.UpdateBuffer(_indexBuffer, 0, CollectionsMarshal.AsSpan(_indices));
        commandList.DrawIndexed((uint)_indices.Count, 1, 0, 0, 0);
    }

    private Vector2 ToNdc(float x, float y)
    {
        float ndcX = (x / _width) * 2f - 1f;
        float ndcY = 1f - (y / _height) * 2f;
        return new Vector2(ndcX, ndcY);
    }

    private static ShaderSetDescription CreateShaders(GraphicsDevice device)
    {
        if (device.BackendType == GraphicsBackend.Direct3D11)
        {
            return CreateShadersHlsl(device);
        }

        const string vertexCode = @"#version 450
layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 UV;
layout(location = 2) in vec4 Color;

layout(location = 0) out vec2 fsin_UV;
layout(location = 1) out vec4 fsin_Color;

void main()
{
    fsin_UV = UV;
    fsin_Color = Color;
    gl_Position = vec4(Position.xy, 0.0, 1.0);
}
";

        const string fragmentCode = @"#version 450
layout(set = 0, binding = 0) uniform texture2D SpriteTexture;
layout(set = 0, binding = 1) uniform sampler SpriteSampler;

layout(location = 0) in vec2 fsin_UV;
layout(location = 1) in vec4 fsin_Color;

layout(location = 0) out vec4 fsout_Color;

vec3 LinearToSrgb(vec3 c)
{
    vec3 lo = 12.92 * c;
    vec3 hi = 1.055 * pow(c, vec3(1.0 / 2.4)) - 0.055;
    vec3 useHi = step(vec3(0.0031308), c);
    return mix(lo, hi, useHi);
}

void main()
{
    vec4 tex = texture(sampler2D(SpriteTexture, SpriteSampler), fsin_UV);
    vec4 color = tex * fsin_Color;
    color.rgb = LinearToSrgb(color.rgb);
    fsout_Color = color;
}
";

        var factory = device.ResourceFactory;
        var shaders = factory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));

        return new ShaderSetDescription(new[] { SpriteVertex.Layout }, shaders);
    }

    private static ShaderSetDescription CreateShadersHlsl(GraphicsDevice device)
    {
        const string vertexCode = @"
struct VSInput
{
    float2 Position : POSITION0;
    float2 UV       : TEXCOORD0;
    float4 Color    : COLOR0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float2 UV       : TEXCOORD0;
    float4 Color    : COLOR0;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.Position = float4(input.Position.xy, 0.0, 1.0);
    output.UV = input.UV;
    output.Color = input.Color;
    return output;
}";

        const string fragmentCode = @"
Texture2D SpriteTexture : register(t0);
SamplerState SpriteSampler : register(s0);

struct PSInput
{
    float4 Position : SV_Position;
    float2 UV       : TEXCOORD0;
    float4 Color    : COLOR0;
};

float3 LinearToSrgb(float3 c)
{
    float3 lo = 12.92 * c;
    float3 hi = 1.055 * pow(c, 1.0 / 2.4) - 0.055;
    float3 useHi = step(0.0031308, c);
    return lerp(lo, hi, useHi);
}

float4 main(PSInput input) : SV_Target
{
    float4 tex = SpriteTexture.Sample(SpriteSampler, input.UV);
    float4 color = tex * input.Color;
    color.rgb = LinearToSrgb(color.rgb);
    return color;
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

        return new ShaderSetDescription(new[] { SpriteVertex.Layout }, new[] { vertex, fragment });
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _pipeline.Dispose();
        _layout.Dispose();
        _set.Dispose();
    }
}
