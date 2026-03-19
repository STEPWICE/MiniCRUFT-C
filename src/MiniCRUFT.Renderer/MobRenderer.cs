using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using MiniCRUFT.Core;
using Veldrid;
using Veldrid.SPIRV;

namespace MiniCRUFT.Renderer;

public sealed class MobRenderer : IDisposable
{
    private readonly DeviceBuffer _vertexBuffer;
    private readonly DeviceBuffer _indexBuffer;
    private readonly ResourceLayout _resourceLayout;
    private readonly ResourceSet _resourceSet;
    private readonly Pipeline _pipeline;
    private readonly Pipeline _emissiveOverlayPipeline;
    private readonly SpriteAtlas _atlas;
    private readonly Dictionary<string, SpriteRegion> _textureRegions = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<global::MiniCRUFT.Renderer.MobVertex> _vertices = new();
    private readonly List<ushort> _indices = new();
    private readonly List<global::MiniCRUFT.Renderer.MobVertex> _emissiveVertices = new();
    private readonly List<ushort> _emissiveIndices = new();

    public MobRenderer(GraphicsDevice device, AssetStore assets, DeviceBuffer cameraBuffer)
    {
        _vertexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(512 * 1024, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _indexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(256 * 1024, BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        var sources = MobModelCatalog.GetTextureSources();
        _atlas = SpriteAtlas.Build(device, assets, sources, repeatSampler: false);
        foreach (var source in sources)
        {
            _textureRegions[source.Name] = _atlas.GetRegion(source.Name);
        }

        _resourceLayout = device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("CameraBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment),
            new ResourceLayoutElementDescription("MobTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("MobSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        _resourceSet = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _resourceLayout,
            cameraBuffer,
            _atlas.Texture,
            _atlas.Sampler));

        var shaderSet = CreateShaders(device, fullBrightOverlay: false);
        var pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleDisabled,
            DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: true,
                comparisonKind: ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { _resourceLayout },
            ShaderSet = shaderSet,
            Outputs = device.MainSwapchain.Framebuffer.OutputDescription
        };

        _pipeline = device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);

        var emissiveShaderSet = CreateShaders(device, fullBrightOverlay: true);
        var emissivePipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = new DepthStencilStateDescription(
                depthTestEnabled: true,
                depthWriteEnabled: false,
                comparisonKind: ComparisonKind.LessEqual),
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { _resourceLayout },
            ShaderSet = emissiveShaderSet,
            Outputs = device.MainSwapchain.Framebuffer.OutputDescription
        };

        _emissiveOverlayPipeline = device.ResourceFactory.CreateGraphicsPipeline(emissivePipelineDescription);
    }

    public void Draw(CommandList commandList, Camera camera, CameraUniform uniform, IReadOnlyList<MobRenderInstance> mobs)
    {
        Draw(commandList, camera, mobs);
    }

    public void Draw(CommandList commandList, Camera camera, IReadOnlyList<MobRenderInstance> mobs)
    {
        if (mobs.Count == 0)
        {
            return;
        }

        _vertices.Clear();
        _indices.Clear();
        _emissiveVertices.Clear();
        _emissiveIndices.Clear();

        for (int i = 0; i < mobs.Count; i++)
        {
            var mob = mobs[i];
            var model = MobModelCatalog.Get(mob.Type);
            if (!_textureRegions.TryGetValue(model.TextureName, out var region))
            {
                continue;
            }

            AddMob(_vertices, _indices, mob, model, region);

            if (!string.IsNullOrEmpty(model.OverlayTextureName) &&
                _textureRegions.TryGetValue(model.OverlayTextureName, out var overlayRegion))
            {
                if (model.OverlayFullBright)
                {
                    AddMob(_emissiveVertices, _emissiveIndices, mob, model, overlayRegion, overlayPass: true);
                }
                else
                {
                    AddMob(_vertices, _indices, mob, model, overlayRegion, overlayPass: true);
                }
            }
        }

        if (_indices.Count == 0)
        {
            if (_emissiveIndices.Count == 0)
            {
                return;
            }
        }

        commandList.SetGraphicsResourceSet(0, _resourceSet);
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);

        if (_indices.Count > 0)
        {
            commandList.SetPipeline(_pipeline);
            commandList.UpdateBuffer(_vertexBuffer, 0, CollectionsMarshal.AsSpan(_vertices));
            commandList.UpdateBuffer(_indexBuffer, 0, CollectionsMarshal.AsSpan(_indices));
            commandList.DrawIndexed((uint)_indices.Count, 1, 0, 0, 0);
        }

        if (_emissiveIndices.Count > 0)
        {
            commandList.SetPipeline(_emissiveOverlayPipeline);
            commandList.UpdateBuffer(_vertexBuffer, 0, CollectionsMarshal.AsSpan(_emissiveVertices));
            commandList.UpdateBuffer(_indexBuffer, 0, CollectionsMarshal.AsSpan(_emissiveIndices));
            commandList.DrawIndexed((uint)_emissiveIndices.Count, 1, 0, 0, 0);
        }
    }

    private static void AddMob(List<MobVertex> vertices, List<ushort> indices, MobRenderInstance mob, MobModelDefinition model, SpriteRegion region, bool overlayPass = false)
    {
        MobModelBuilder.AppendMob(vertices, indices, mob, region, model, overlayPass);
    }

    private static ShaderSetDescription CreateShaders(GraphicsDevice device, bool fullBrightOverlay)
    {
        if (device.BackendType == GraphicsBackend.Direct3D11)
        {
            return CreateShadersHlsl(device, fullBrightOverlay);
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
layout(location = 1) in vec2 UV;
layout(location = 2) in vec4 Tint;
layout(location = 3) in vec2 Effects;

layout(location = 0) out vec2 fsin_UV;
layout(location = 1) out vec4 fsin_Tint;
layout(location = 2) out vec2 fsin_Effects;
layout(location = 3) out vec3 fsin_WorldPos;

void main()
{
    fsin_UV = UV;
    fsin_Tint = Tint;
    fsin_Effects = Effects;
    fsin_WorldPos = Position;
    gl_Position = ViewProjection * vec4(Position, 1.0);
}
";

        const string fragmentPreamble = @"#version 450
layout(set = 0, binding = 1) uniform texture2D MobTexture;
layout(set = 0, binding = 2) uniform sampler MobSampler;

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

layout(location = 0) in vec2 fsin_UV;
layout(location = 1) in vec4 fsin_Tint;
layout(location = 2) in vec2 fsin_Effects;
layout(location = 3) in vec3 fsin_WorldPos;

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
    vec4 tex = texture(sampler2D(MobTexture, MobSampler), fsin_UV);
    float alpha = tex.a * fsin_Tint.a;
    if (alpha < Misc.x)
    {
        discard;
    }

    vec3 rgb = tex.rgb * fsin_Tint.rgb;
";

        string lightingBlock = fullBrightOverlay
            ? @"
"
            : @"    float sun = clamp(CameraPos.w, 0.05, 1.0);
    float light = mix(LightingParams.y, 1.0, sun);
    light = max(light, LightingParams.x);

    rgb *= light;
    float hurt = clamp(fsin_Effects.x, 0.0, 1.0);
    float special = clamp(fsin_Effects.y, 0.0, 1.0);
    rgb = mix(rgb, vec3(1.0, 0.2, 0.2), hurt * 0.7);
    rgb = mix(rgb, vec3(1.0), special * 0.6);
";

        string fogBlock = @"

    float dist = length(fsin_WorldPos - CameraPos.xyz);
    float horizonMask = smoothstep(FogParams.x, FogParams.y, dist);
    float fogFactor = ComputeFogFactor(dist);
    fogFactor = ApplyHorizon(fogFactor, fsin_WorldPos, CameraPos.xyz, HorizonParams.x, HorizonParams.y, horizonMask);

    rgb = mix(FogColor.rgb, rgb, fogFactor);
    rgb = LinearToSrgb(rgb);
    fsout_Color = vec4(rgb, alpha);
}
";
        string fragmentCode = fragmentPreamble + lightingBlock + fogBlock;
        var factory = device.ResourceFactory;
        var shaders = factory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));

        return new ShaderSetDescription(new[] { global::MiniCRUFT.Renderer.MobVertex.Layout }, shaders);
    }

    private static ShaderSetDescription CreateShadersHlsl(GraphicsDevice device, bool fullBrightOverlay)
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
    float2 UV       : TEXCOORD0;
    float4 Tint     : TEXCOORD1;
    float2 Effects  : TEXCOORD2;
};

struct VSOutput
{
    float4 Position : SV_Position;
    float2 UV       : TEXCOORD0;
    float4 Tint     : TEXCOORD1;
    float2 Effects  : TEXCOORD2;
    float3 WorldPos : TEXCOORD3;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.UV = input.UV;
    output.Tint = input.Tint;
    output.Effects = input.Effects;
    output.WorldPos = input.Position;
    output.Position = mul(float4(input.Position, 1.0), ViewProjection);
    return output;
}";

        const string fragmentHeader = @"
Texture2D MobTexture : register(t0);
SamplerState MobSampler : register(s0);

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

struct PSInput
{
    float4 Position : SV_Position;
    float2 UV       : TEXCOORD0;
    float4 Tint     : TEXCOORD1;
    float2 Effects  : TEXCOORD2;
    float3 WorldPos : TEXCOORD3;
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

";

        string fragmentBody = fullBrightOverlay
            ? @"float4 main(PSInput input) : SV_Target
{
    float4 tex = MobTexture.Sample(MobSampler, input.UV);
    float alpha = tex.a * input.Tint.a;
    if (alpha < Misc.x)
    {
        discard;
    }

    float3 rgb = tex.rgb * input.Tint.rgb;

    float dist = length(input.WorldPos - CameraPos.xyz);
    float horizonMask = smoothstep(FogParams.x, FogParams.y, dist);
    float fogFactor = ComputeFogFactor(dist);
    fogFactor = ApplyHorizon(fogFactor, input.WorldPos, CameraPos.xyz, HorizonParams.x, HorizonParams.y, horizonMask);

    rgb = lerp(FogColor.rgb, rgb, fogFactor);
    rgb = LinearToSrgb(rgb);
    return float4(rgb, alpha);
}"
            : @"float4 main(PSInput input) : SV_Target
{
    float4 tex = MobTexture.Sample(MobSampler, input.UV);
    float alpha = tex.a * input.Tint.a;
    if (alpha < Misc.x)
    {
        discard;
    }

    float sun = clamp(CameraPos.w, 0.05, 1.0);
    float light = max(lerp(LightingParams.y, 1.0, sun), LightingParams.x);

    float3 rgb = tex.rgb * input.Tint.rgb * light;
    float hurt = saturate(input.Effects.x);
    float special = saturate(input.Effects.y);
    rgb = lerp(rgb, float3(1.0, 0.2, 0.2), hurt * 0.7);
    rgb = lerp(rgb, float3(1.0, 1.0, 1.0), special * 0.6);

    float dist = length(input.WorldPos - CameraPos.xyz);
    float horizonMask = smoothstep(FogParams.x, FogParams.y, dist);
    float fogFactor = ComputeFogFactor(dist);
    fogFactor = ApplyHorizon(fogFactor, input.WorldPos, CameraPos.xyz, HorizonParams.x, HorizonParams.y, horizonMask);

    rgb = lerp(FogColor.rgb, rgb, fogFactor);
    rgb = LinearToSrgb(rgb);
    return float4(rgb, alpha);
}";

        string fragmentCode = fragmentHeader + fragmentBody;

        var factory = device.ResourceFactory;
        var vertex = factory.CreateShader(new ShaderDescription(
            ShaderStages.Vertex,
            Encoding.UTF8.GetBytes(vertexCode),
            "main"));
        var fragment = factory.CreateShader(new ShaderDescription(
            ShaderStages.Fragment,
            Encoding.UTF8.GetBytes(fragmentCode),
            "main"));

        return new ShaderSetDescription(new[] { global::MiniCRUFT.Renderer.MobVertex.Layout }, new[] { vertex, fragment });
    }

    public void Dispose()
    {
        _emissiveOverlayPipeline.Dispose();
        _pipeline.Dispose();
        _resourceSet.Dispose();
        _resourceLayout.Dispose();
        _indexBuffer.Dispose();
        _vertexBuffer.Dispose();
        _atlas.Dispose();
    }

}
