using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using FontStashSharp;
using MiniCRUFT.Core;
using Veldrid;
using Veldrid.SPIRV;

namespace MiniCRUFT.Renderer;

public sealed class UiTextRenderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    private readonly Pipeline _pipeline;
    private readonly ResourceLayout _resourceLayout;
    private readonly Sampler _sampler;

    private readonly Dictionary<TextureView, ResourceSet> _resourceSets = new();
    private readonly HashSet<FontTexture> _initialized = new();
    private readonly Dictionary<FontTexture, TextBatch> _batches = new();

    private int _width;
    private int _height;

    public UiTextRenderer(GraphicsDevice device)
    {
        _device = device;
        _vertexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(512 * 1024, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _indexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(256 * 1024, BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        _resourceLayout = device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("FontTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("FontSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        _sampler = device.ResourceFactory.CreateSampler(new SamplerDescription
        {
            AddressModeU = SamplerAddressMode.Clamp,
            AddressModeV = SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.MinPoint_MagPoint_MipPoint
        });

        var shaderSet = CreateShaders(device);
        _pipeline = device.ResourceFactory.CreateGraphicsPipeline(new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = DepthStencilStateDescription.Disabled,
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { _resourceLayout },
            ShaderSet = shaderSet,
            Outputs = device.MainSwapchain.Framebuffer.OutputDescription
        });
    }

    private static ShaderSetDescription CreateShaders(GraphicsDevice device)
    {
        if (device.BackendType == GraphicsBackend.Direct3D11)
        {
            return CreateShadersHlsl(device);
        }

        const string vertexCode = @"#version 450
layout(location = 0) in vec2 Position;
layout(location = 1) in vec2 TexCoord;
layout(location = 2) in vec4 Color;

layout(location = 0) out vec2 fsin_TexCoord;
layout(location = 1) out vec4 fsin_Color;

void main()
{
    fsin_TexCoord = TexCoord;
    fsin_Color = Color;
    gl_Position = vec4(Position.xy, 0.0, 1.0);
}
";

        const string fragmentCode = @"#version 450
layout(set = 0, binding = 0) uniform texture2D FontTexture;
layout(set = 0, binding = 1) uniform sampler FontSampler;

layout(location = 0) in vec2 fsin_TexCoord;
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
    float alpha = texture(sampler2D(FontTexture, FontSampler), fsin_TexCoord).r;
    vec3 rgb = LinearToSrgb(fsin_Color.rgb);
    fsout_Color = vec4(rgb, fsin_Color.a * alpha);
    if (fsout_Color.a < 0.05)
    {
        discard;
    }
}
";

        var factory = device.ResourceFactory;
        var shaders = factory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));

        return new ShaderSetDescription(new[] { UiTextVertex.Layout }, shaders);
    }

    private static ShaderSetDescription CreateShadersHlsl(GraphicsDevice device)
    {
        const string vertexCode = @"
struct VSInput
{
    float2 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color    : COLOR0;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
    float4 Color    : COLOR0;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.Position = float4(input.Position.xy, 0.0, 1.0);
    output.TexCoord = input.TexCoord;
    output.Color = input.Color;
    return output;
}";

        const string fragmentCode = @"
Texture2D FontTexture : register(t0);
SamplerState FontSampler : register(s0);

struct PSInput
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
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
    float alpha = FontTexture.Sample(FontSampler, input.TexCoord).r;
    float3 rgb = LinearToSrgb(input.Color.rgb);
    float4 color = float4(rgb, input.Color.a * alpha);
    if (color.a < 0.05)
    {
        discard;
    }
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

        return new ShaderSetDescription(new[] { UiTextVertex.Layout }, new[] { vertex, fragment });
    }

    public void Begin(int width, int height)
    {
        _width = width;
        _height = height;
        foreach (var batch in _batches.Values)
        {
            batch.Vertices.Clear();
            batch.Indices.Clear();
        }
    }

    public void Queue(FontTexture texture, Vector2 position, System.Drawing.Rectangle? sourceRect, FSColor color, Vector2 scale)
    {
        if (!_batches.TryGetValue(texture, out var batch))
        {
            batch = new TextBatch(texture);
            _batches[texture] = batch;
        }
        if (_initialized.Add(texture))
        {
            texture.Clear();
        }

        var rect = sourceRect ?? new System.Drawing.Rectangle(0, 0, texture.Width, texture.Height);
        float width = rect.Width * scale.X;
        float height = rect.Height * scale.Y;

        Vector2 p0 = ToNdc(position.X, position.Y);
        Vector2 p1 = ToNdc(position.X + width, position.Y);
        Vector2 p2 = ToNdc(position.X + width, position.Y + height);
        Vector2 p3 = ToNdc(position.X, position.Y + height);

        Vector2 uv0 = new Vector2(rect.X / (float)texture.Width, rect.Y / (float)texture.Height);
        Vector2 uv1 = new Vector2((rect.X + rect.Width) / (float)texture.Width, rect.Y / (float)texture.Height);
        Vector2 uv2 = new Vector2((rect.X + rect.Width) / (float)texture.Width, (rect.Y + rect.Height) / (float)texture.Height);
        Vector2 uv3 = new Vector2(rect.X / (float)texture.Width, (rect.Y + rect.Height) / (float)texture.Height);

        var tint = ColorSpace.ToLinear(new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));

        ushort start = (ushort)batch.Vertices.Count;
        batch.Vertices.Add(new UiTextVertex(p0, uv0, tint));
        batch.Vertices.Add(new UiTextVertex(p1, uv1, tint));
        batch.Vertices.Add(new UiTextVertex(p2, uv2, tint));
        batch.Vertices.Add(new UiTextVertex(p3, uv3, tint));

        batch.Indices.Add(start);
        batch.Indices.Add((ushort)(start + 1));
        batch.Indices.Add((ushort)(start + 2));
        batch.Indices.Add(start);
        batch.Indices.Add((ushort)(start + 2));
        batch.Indices.Add((ushort)(start + 3));
    }

    public void Flush(CommandList commandList)
    {
        foreach (var batch in _batches.Values)
        {
            if (batch.Indices.Count == 0)
            {
                continue;
            }

            EnsureCapacity(batch.Vertices.Count, batch.Indices.Count);

            commandList.SetPipeline(_pipeline);

            var resourceSet = GetResourceSet(batch.Texture.View);
            commandList.SetGraphicsResourceSet(0, resourceSet);

            commandList.SetVertexBuffer(0, _vertexBuffer);
            commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            commandList.UpdateBuffer(_vertexBuffer, 0, batch.Vertices.ToArray());
            commandList.UpdateBuffer(_indexBuffer, 0, batch.Indices.ToArray());
            commandList.DrawIndexed((uint)batch.Indices.Count, 1, 0, 0, 0);
        }
    }

    private ResourceSet GetResourceSet(TextureView view)
    {
        if (_resourceSets.TryGetValue(view, out var set))
        {
            return set;
        }

        set = _device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_resourceLayout, view, _sampler));
        _resourceSets[view] = set;
        return set;
    }

    private void EnsureCapacity(int vertexCount, int indexCount)
    {
        uint vertexSize = (uint)(vertexCount * System.Runtime.InteropServices.Marshal.SizeOf<UiTextVertex>());
        uint indexSize = (uint)(indexCount * sizeof(ushort));

        if (vertexSize > _vertexBuffer.SizeInBytes)
        {
            _vertexBuffer.Dispose();
            _vertexBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(vertexSize, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        if (indexSize > _indexBuffer.SizeInBytes)
        {
            _indexBuffer.Dispose();
            _indexBuffer = _device.ResourceFactory.CreateBuffer(new BufferDescription(indexSize, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        }
    }

    private Vector2 ToNdc(float x, float y)
    {
        float ndcX = (x / _width) * 2f - 1f;
        float ndcY = 1f - (y / _height) * 2f;
        return new Vector2(ndcX, ndcY);
    }

    public void Dispose()
    {
        foreach (var set in _resourceSets.Values)
        {
            set.Dispose();
        }
        _resourceSets.Clear();

        _sampler.Dispose();
        _pipeline.Dispose();
        _resourceLayout.Dispose();
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }

    private sealed class TextBatch
    {
        public FontTexture Texture { get; }
        public List<UiTextVertex> Vertices { get; } = new();
        public List<ushort> Indices { get; } = new();

        public TextBatch(FontTexture texture)
        {
            Texture = texture;
        }
    }
}
