using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using Veldrid;
using Veldrid.SPIRV;

namespace MiniCRUFT.Renderer;

public sealed class SelectionOverlayRenderer : IDisposable
{
    private readonly DeviceBuffer _vertexBuffer;
    private readonly DeviceBuffer _indexBuffer;
    private readonly ResourceLayout _resourceLayout;
    private readonly ResourceSet _resourceSet;
    private readonly Pipeline _pipeline;
    private readonly List<SelectionVertex> _vertices = new();
    private readonly List<ushort> _indices = new();

    public SelectionOverlayRenderer(GraphicsDevice device, DeviceBuffer cameraBuffer)
    {
        _vertexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(64 * 1024, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _indexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(32 * 1024, BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        _resourceLayout = device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("CameraBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

        _resourceSet = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(_resourceLayout, cameraBuffer));

        var shaderSet = CreateShaders(device);
        var pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: false,
                comparisonKind: ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.LineList,
            ResourceLayouts = new[] { _resourceLayout },
            ShaderSet = shaderSet,
            Outputs = device.MainSwapchain.Framebuffer.OutputDescription
        };

        _pipeline = device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);
    }

    public void Draw(CommandList commandList, SelectionState selection)
    {
        if (!selection.HasSelection)
        {
            return;
        }

        _vertices.Clear();
        _indices.Clear();

        switch (selection.Kind)
        {
            case SelectionKind.Block:
                AddBlock(selection.Block, selection.Progress);
                break;
            case SelectionKind.Mob:
                AddMob(selection.Mob, selection.Distance);
                break;
        }

        if (_indices.Count == 0)
        {
            return;
        }

        commandList.SetPipeline(_pipeline);
        commandList.SetGraphicsResourceSet(0, _resourceSet);
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        commandList.UpdateBuffer(_vertexBuffer, 0, CollectionsMarshal.AsSpan(_vertices));
        commandList.UpdateBuffer(_indexBuffer, 0, CollectionsMarshal.AsSpan(_indices));
        commandList.DrawIndexed((uint)_indices.Count, 1, 0, 0, 0);
    }

    private void AddBlock(BlockCoord block, float progress)
    {
        var min = new Vector3(block.X, block.Y, block.Z) - new Vector3(0.0025f);
        var max = new Vector3(block.X + 1f, block.Y + 1f, block.Z + 1f) + new Vector3(0.0025f);
        float t = Math.Clamp(progress, 0f, 1f);
        var baseColor = new Vector4(1f, 1f, 1f, 0.95f);
        var activeColor = new Vector4(1f, 0.72f, 0.22f, 1f);
        AddBox(min, max, Vector4.Lerp(baseColor, activeColor, t));
    }

    private void AddMob(MobRenderInstance mob, float distance)
    {
        float halfWidth = Math.Max(0.1f, mob.Width * 0.5f);
        float height = Math.Max(0.1f, mob.Height);
        var min = new Vector3(mob.Position.X - halfWidth, mob.Position.Y, mob.Position.Z - halfWidth) - new Vector3(0.02f);
        var max = new Vector3(mob.Position.X + halfWidth, mob.Position.Y + height, mob.Position.Z + halfWidth) + new Vector3(0.02f);

        float distanceFade = 1f - Math.Clamp(distance / 24f, 0f, 0.35f);
        float hurt = Math.Clamp(mob.HurtFlash, 0f, 1f);
        var baseColor = new Vector4(1f, 1f, 1f, 0.85f * distanceFade);
        var hurtColor = new Vector4(1f, 0.35f, 0.25f, 0.95f * distanceFade);
        var color = Vector4.Lerp(baseColor, hurtColor, hurt);
        AddBox(min, max, color);
    }

    private void AddBox(Vector3 min, Vector3 max, Vector4 color)
    {
        if (_vertices.Count > ushort.MaxValue - 8)
        {
            return;
        }

        ushort start = (ushort)_vertices.Count;

        Vector3 p000 = new(min.X, min.Y, min.Z);
        Vector3 p100 = new(max.X, min.Y, min.Z);
        Vector3 p110 = new(max.X, max.Y, min.Z);
        Vector3 p010 = new(min.X, max.Y, min.Z);
        Vector3 p001 = new(min.X, min.Y, max.Z);
        Vector3 p101 = new(max.X, min.Y, max.Z);
        Vector3 p111 = new(max.X, max.Y, max.Z);
        Vector3 p011 = new(min.X, max.Y, max.Z);

        _vertices.Add(new SelectionVertex(p000, color));
        _vertices.Add(new SelectionVertex(p100, color));
        _vertices.Add(new SelectionVertex(p110, color));
        _vertices.Add(new SelectionVertex(p010, color));
        _vertices.Add(new SelectionVertex(p001, color));
        _vertices.Add(new SelectionVertex(p101, color));
        _vertices.Add(new SelectionVertex(p111, color));
        _vertices.Add(new SelectionVertex(p011, color));

        AddLine(start, start + 1);
        AddLine(start + 1, start + 2);
        AddLine(start + 2, start + 3);
        AddLine(start + 3, start);

        AddLine(start + 4, start + 5);
        AddLine(start + 5, start + 6);
        AddLine(start + 6, start + 7);
        AddLine(start + 7, start + 4);

        AddLine(start, start + 4);
        AddLine(start + 1, start + 5);
        AddLine(start + 2, start + 6);
        AddLine(start + 3, start + 7);
    }

    private void AddLine(int a, int b)
    {
        _indices.Add((ushort)a);
        _indices.Add((ushort)b);
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
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec4 Color;

layout(location = 0) out vec4 fsin_Color;

void main()
{
    fsin_Color = Color;
    gl_Position = ViewProjection * vec4(Position, 1.0);
}
";

        const string fragmentCode = @"#version 450
layout(location = 0) in vec4 fsin_Color;
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
    vec4 color = fsin_Color;
    color.rgb = LinearToSrgb(color.rgb);
    fsout_Color = color;
}
";

        var factory = device.ResourceFactory;
        var shaders = factory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));

        return new ShaderSetDescription(new[] { SelectionVertex.Layout }, shaders);
    }

    private static ShaderSetDescription CreateShadersHlsl(GraphicsDevice device)
    {
        const string vertexCode = @"
cbuffer CameraBuffer : register(b0)
{
    row_major float4x4 ViewProjection;
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
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.Position = mul(float4(input.Position, 1.0), ViewProjection);
    output.Color = input.Color;
    return output;
}";

        const string fragmentCode = @"
struct PSInput
{
    float4 Position : SV_Position;
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
    float4 color = input.Color;
    color.rgb = LinearToSrgb(color.rgb);
    return color;
}";

        var factory = device.ResourceFactory;
        var vertex = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"));
        var fragment = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));

        return new ShaderSetDescription(new[] { SelectionVertex.Layout }, new[] { vertex, fragment });
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _resourceSet.Dispose();
        _resourceLayout.Dispose();
        _pipeline.Dispose();
    }
}
