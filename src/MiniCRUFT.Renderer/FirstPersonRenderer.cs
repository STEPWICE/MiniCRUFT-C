using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using MiniCRUFT.Core;
using MiniCRUFT.World;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.SPIRV;

namespace MiniCRUFT.Renderer;

public sealed class FirstPersonRenderer : IDisposable
{
    private const float FaceTopShade = 1.0f;
    private const float FaceSideShade = 0.88f;
    private const float FaceFrontShade = 0.94f;
    private const float FaceBottomShade = 0.72f;
    private const float PivotEpsilon = 0.0001f;

    private readonly DeviceBuffer _vertexBuffer;
    private readonly DeviceBuffer _indexBuffer;
    private readonly ResourceLayout _resourceLayout;
    private readonly ResourceSet _handResourceSet;
    private readonly ResourceSet _itemResourceSet;
    private readonly Pipeline _pipeline;
    private readonly TextureAtlas _handAtlas;
    private readonly TextureAtlas _blockAtlas;
    private readonly FirstPersonConfig _config;
    private readonly List<FirstPersonVertex> _vertices = new();
    private readonly List<ushort> _indices = new();

    public FirstPersonRenderer(GraphicsDevice device, DeviceBuffer cameraBuffer, TextureAtlas blockAtlas, RenderConfig renderConfig, FirstPersonConfig config)
    {
        BlockRegistry.Initialize();

        _blockAtlas = blockAtlas;
        _config = config;

        _vertexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(256 * 1024, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _indexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(128 * 1024, BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        _handAtlas = CreateHandAtlas(device);

        _resourceLayout = device.ResourceFactory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("CameraBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("AtlasTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("AtlasSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

        _handResourceSet = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _resourceLayout,
            cameraBuffer,
            _handAtlas.Texture,
            _handAtlas.Sampler));

        _itemResourceSet = device.ResourceFactory.CreateResourceSet(new ResourceSetDescription(
            _resourceLayout,
            cameraBuffer,
            _blockAtlas.Texture,
            _blockAtlas.Sampler));

        var shaderSet = CreateShaders(device, renderConfig.CutoutAlphaThreshold);
        _pipeline = CreatePipeline(device, shaderSet);
    }

    public void Draw(CommandList commandList, Camera camera, FirstPersonRenderState state)
    {
        if (!_config.Enabled || !state.Visible)
        {
            return;
        }

        Vector3 cameraPos = camera.Position;
        Vector3 right = camera.GetRight();
        Vector3 forward = camera.GetForward();
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward));

        float swingProgress = Math.Clamp(state.SwingProgress, 0f, 1f);
        float swingRoot = swingProgress <= 0f ? 0f : MathF.Sin(MathF.Sqrt(swingProgress) * MathF.PI);
        float swingCurvePower = Math.Max(0.01f, _config.SwingCurvePower);
        float swingCurve = swingProgress <= 0f ? 0f : MathF.Sin(MathF.Pow(swingProgress, swingCurvePower) * MathF.PI);
        float swingWave = swingProgress <= 0f ? 0f : MathF.Sin(swingProgress * MathF.PI);
        float movement = Math.Clamp(state.MovementStrength, 0f, 1f);
        float airFactor = state.OnGround ? 1f : 0.35f;
        float motionFactor = movement * airFactor;
        float movementWave = MathF.Sin(state.WorldTimeSeconds * _config.MovementBobSpeed);
        float idleWave = MathF.Sin(state.WorldTimeSeconds * _config.IdleBobSpeed);

        _vertices.Clear();
        _indices.Clear();
        BuildArm(cameraPos, right, up, forward, swingRoot, swingCurve, swingWave, motionFactor, movementWave, idleWave);
        FlushPass(commandList, _handResourceSet);

        if (state.HeldBlock == BlockId.Air)
        {
            return;
        }

        _vertices.Clear();
        _indices.Clear();
        BuildHeldItem(cameraPos, right, up, forward, state.HeldBlock, swingRoot, swingCurve, swingWave, motionFactor, movementWave, idleWave);
        FlushPass(commandList, _itemResourceSet);
    }

    private void BuildArm(Vector3 cameraPos, Vector3 right, Vector3 up, Vector3 forward, float swingRoot, float swingCurve, float swingWave, float motionFactor, float movementWave, float idleWave)
    {
        float handMotionScale = Math.Max(0.01f, _config.HandMotionScale);
        float handSwingScale = Math.Max(0.01f, _config.HandSwingScale);
        var origin = cameraPos
            + forward * _config.HandForwardOffset
            + right * _config.HandRightOffset
            - up * _config.HandDownOffset;

        origin += right * (_config.MovementBobAmplitude * motionFactor * movementWave * handMotionScale);
        origin += up * (((_config.IdleBobAmplitude * idleWave) + (_config.MovementBobAmplitude * 0.55f * motionFactor * movementWave)) * handMotionScale);
        origin += forward * (_config.MovementBobAmplitude * 0.25f * motionFactor * movementWave * handMotionScale);
        origin += right * (_config.SwingTranslationX * swingRoot * handSwingScale);
        origin += up * (_config.SwingTranslationY * swingCurve * handSwingScale);
        origin += forward * (_config.SwingTranslationZ * swingWave * handSwingScale);

        var rotation = new Vector3(
            DegreesToRadians(_config.SwingRotationX * swingRoot * handSwingScale + motionFactor * _config.MotionRotationDegrees * movementWave * 0.35f * handMotionScale),
            DegreesToRadians(_config.SwingRotationY * swingCurve * handSwingScale + motionFactor * _config.MotionRotationDegrees * movementWave * 0.55f * handMotionScale),
            DegreesToRadians(_config.SwingRotationZ * swingWave * handSwingScale + motionFactor * _config.MotionRotationDegrees * movementWave * 0.75f * handMotionScale));

        var pose = new FirstPersonPose(origin, rotation, new Vector3(_config.HandWidth, _config.HandHeight, _config.HandDepth));
        var region = _handAtlas.GetRegion("hand");
        var tint = new Vector4(1f, 0.95f, 0.92f, 1f);

        AddBox(
            pose,
            right,
            up,
            forward,
            new Vector3(-0.5f, -1f, -0.5f),
            new Vector3(0.5f, 0f, 0.5f),
            _ => region,
            tint);
    }

    private void BuildHeldItem(Vector3 cameraPos, Vector3 right, Vector3 up, Vector3 forward, BlockId heldBlock, float swingRoot, float swingCurve, float swingWave, float motionFactor, float movementWave, float idleWave)
    {
        var definition = BlockRegistry.Get(heldBlock);
        bool transparentItem = definition.RenderMode == RenderMode.Transparent;
        bool crossItem = definition.RenderMode == RenderMode.Cross;
        bool torchItem = definition.RenderMode == RenderMode.Torch;
        bool cardItem = crossItem;
        float itemScale = torchItem
            ? _config.TorchScale
            : crossItem
                ? _config.CrossScale
                : transparentItem
                    ? _config.TransparentScale
            : _config.ItemScale;
        float itemMotionScale = Math.Max(0.01f, _config.ItemMotionScale);
        float itemSwingScale = Math.Max(0.01f, _config.ItemSwingScale);
        float translationScale = torchItem ? 0.9f : cardItem ? 0.8f : transparentItem ? 0.75f : 1f;
        float rotationScale = torchItem ? 0.65f : cardItem ? 0.45f : transparentItem ? 0.55f : 1f;

        var origin = cameraPos
            + forward * _config.ItemForwardOffset
            + right * _config.ItemRightOffset
            - up * _config.ItemDownOffset;

        origin += right * (_config.MovementBobAmplitude * motionFactor * movementWave * 1.4f * itemMotionScale);
        origin += up * (((_config.IdleBobAmplitude * idleWave * 0.85f) + (_config.MovementBobAmplitude * 0.45f * motionFactor * movementWave)) * itemMotionScale);
        origin += forward * (_config.MovementBobAmplitude * 0.18f * motionFactor * movementWave * itemMotionScale);
        origin += right * (_config.SwingTranslationX * swingRoot * 1.15f * translationScale * itemSwingScale);
        origin += up * (_config.SwingTranslationY * swingCurve * 1.05f * translationScale * itemSwingScale);
        origin += forward * (_config.SwingTranslationZ * swingWave * 0.95f * translationScale * itemSwingScale);

        var rotation = new Vector3(
            DegreesToRadians(_config.ItemPitch + _config.SwingRotationX * swingRoot * rotationScale * itemSwingScale + motionFactor * _config.MotionRotationDegrees * movementWave * 0.4f * rotationScale * itemMotionScale),
            DegreesToRadians(_config.ItemYaw + _config.SwingRotationY * swingCurve * rotationScale * itemSwingScale + motionFactor * _config.MotionRotationDegrees * movementWave * 0.55f * rotationScale * itemMotionScale),
            DegreesToRadians(_config.ItemRoll + _config.SwingRotationZ * swingWave * rotationScale * itemSwingScale + motionFactor * _config.MotionRotationDegrees * movementWave * 0.7f * rotationScale * itemMotionScale));

        float thickness = Math.Clamp(_config.CardThickness, 0.005f, 0.25f);
        Vector3 min;
        Vector3 max;
        Func<BlockFace, AtlasRegion> regionLookup;

        if (torchItem)
        {
            var torchRegion = _blockAtlas.GetRegion(definition.TextureSide);
            var flameRegion = _blockAtlas.GetRegion("fire_0");

            min = new Vector3(-0.05f, -0.52f, -0.05f);
            max = new Vector3(0.05f, 0.16f, 0.05f);
            regionLookup = _ => torchRegion;
            var torchPose = new FirstPersonPose(origin, rotation, new Vector3(itemScale, itemScale, itemScale));
            AddBox(torchPose, right, up, forward, min, max, regionLookup, Vector4.One);

            min = new Vector3(-0.08f, 0.12f, -0.08f);
            max = new Vector3(0.08f, 0.30f, 0.08f);
            regionLookup = _ => flameRegion;
            AddBox(torchPose, right, up, forward, min, max, regionLookup, Vector4.One);
            return;
        }
        else if (cardItem)
        {
            min = new Vector3(-0.5f, -0.5f, -thickness * 0.5f);
            max = new Vector3(0.5f, 0.5f, thickness * 0.5f);
            var region = _blockAtlas.GetRegion(definition.TextureSide);
            regionLookup = _ => region;
        }
        else
        {
            min = new Vector3(-0.5f, -0.5f, -0.5f);
            max = new Vector3(0.5f, 0.5f, 0.5f);
            regionLookup = face => _blockAtlas.GetRegion(definition.GetTextureName(face));
        }

        var pose = new FirstPersonPose(origin, rotation, new Vector3(itemScale, itemScale, itemScale));
        AddBox(pose, right, up, forward, min, max, regionLookup, Vector4.One);
    }

    private void AddBox(FirstPersonPose pose, Vector3 right, Vector3 up, Vector3 forward, Vector3 min, Vector3 max, Func<BlockFace, AtlasRegion> faceRegions, Vector4 tint)
    {
        Vector3 p000 = TransformPoint(new Vector3(min.X, min.Y, min.Z), pose, right, up, forward);
        Vector3 p100 = TransformPoint(new Vector3(max.X, min.Y, min.Z), pose, right, up, forward);
        Vector3 p110 = TransformPoint(new Vector3(max.X, max.Y, min.Z), pose, right, up, forward);
        Vector3 p010 = TransformPoint(new Vector3(min.X, max.Y, min.Z), pose, right, up, forward);
        Vector3 p001 = TransformPoint(new Vector3(min.X, min.Y, max.Z), pose, right, up, forward);
        Vector3 p101 = TransformPoint(new Vector3(max.X, min.Y, max.Z), pose, right, up, forward);
        Vector3 p111 = TransformPoint(new Vector3(max.X, max.Y, max.Z), pose, right, up, forward);
        Vector3 p011 = TransformPoint(new Vector3(min.X, max.Y, max.Z), pose, right, up, forward);

        AddFace(p001, p101, p111, p011, faceRegions(BlockFace.South), tint * FaceFrontShade);
        AddFace(p100, p000, p010, p110, faceRegions(BlockFace.North), tint * FaceFrontShade);
        AddFace(p000, p001, p011, p010, faceRegions(BlockFace.West), tint * FaceSideShade);
        AddFace(p101, p100, p110, p111, faceRegions(BlockFace.East), tint * FaceSideShade);
        AddFace(p010, p011, p111, p110, faceRegions(BlockFace.Top), tint * FaceTopShade);
        AddFace(p000, p100, p101, p001, faceRegions(BlockFace.Bottom), tint * FaceBottomShade);
    }

    private void AddFace(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, AtlasRegion region, Vector4 tint)
    {
        if (_vertices.Count > ushort.MaxValue - 4)
        {
            return;
        }

        ushort start = (ushort)_vertices.Count;
        Vector2 atlasMin = region.Min;
        Vector2 atlasSize = region.Max - region.Min;

        _vertices.Add(new FirstPersonVertex(p0, new Vector2(0f, 0f), atlasMin, atlasSize, tint));
        _vertices.Add(new FirstPersonVertex(p1, new Vector2(1f, 0f), atlasMin, atlasSize, tint));
        _vertices.Add(new FirstPersonVertex(p2, new Vector2(1f, 1f), atlasMin, atlasSize, tint));
        _vertices.Add(new FirstPersonVertex(p3, new Vector2(0f, 1f), atlasMin, atlasSize, tint));

        _indices.Add(start);
        _indices.Add((ushort)(start + 1));
        _indices.Add((ushort)(start + 2));
        _indices.Add(start);
        _indices.Add((ushort)(start + 2));
        _indices.Add((ushort)(start + 3));
    }

    private void FlushPass(CommandList commandList, ResourceSet resourceSet)
    {
        if (_indices.Count == 0)
        {
            return;
        }

        commandList.SetPipeline(_pipeline);
        commandList.SetGraphicsResourceSet(0, resourceSet);
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        commandList.UpdateBuffer(_vertexBuffer, 0, CollectionsMarshal.AsSpan(_vertices));
        commandList.UpdateBuffer(_indexBuffer, 0, CollectionsMarshal.AsSpan(_indices));
        commandList.DrawIndexed((uint)_indices.Count, 1, 0, 0, 0);
    }

    private static Vector3 TransformPoint(Vector3 point, FirstPersonPose pose, Vector3 right, Vector3 up, Vector3 forward)
    {
        Vector3 scaled = new(point.X * pose.Scale.X, point.Y * pose.Scale.Y, point.Z * pose.Scale.Z);
        Vector3 rotated = RotateAroundX(scaled, pose.Rotation.X);
        rotated = RotateAroundY(rotated, pose.Rotation.Y);
        rotated = RotateAroundZ(rotated, pose.Rotation.Z);
        return pose.Origin + right * rotated.X + up * rotated.Y + forward * rotated.Z;
    }

    private static Vector3 RotateAroundX(Vector3 point, float angle)
    {
        if (MathF.Abs(angle) <= PivotEpsilon)
        {
            return point;
        }

        float sin = MathF.Sin(angle);
        float cos = MathF.Cos(angle);
        return new Vector3(point.X, point.Y * cos - point.Z * sin, point.Y * sin + point.Z * cos);
    }

    private static Vector3 RotateAroundY(Vector3 point, float angle)
    {
        if (MathF.Abs(angle) <= PivotEpsilon)
        {
            return point;
        }

        float sin = MathF.Sin(angle);
        float cos = MathF.Cos(angle);
        return new Vector3(point.X * cos + point.Z * sin, point.Y, -point.X * sin + point.Z * cos);
    }

    private static Vector3 RotateAroundZ(Vector3 point, float angle)
    {
        if (MathF.Abs(angle) <= PivotEpsilon)
        {
            return point;
        }

        float sin = MathF.Sin(angle);
        float cos = MathF.Cos(angle);
        return new Vector3(point.X * cos - point.Y * sin, point.X * sin + point.Y * cos, point.Z);
    }

    private static float DegreesToRadians(float degrees)
    {
        return MathF.PI * degrees / 180f;
    }

    private static TextureAtlas CreateHandAtlas(GraphicsDevice device)
    {
        const int size = 16;
        var image = new Image<Rgba32>(size, size);
        var baseColor = new Rgba32(198, 160, 130, 255);
        var highlight = new Rgba32(214, 176, 144, 255);
        var shadow = new Rgba32(178, 140, 112, 255);

        for (int y = 0; y < size; y++)
        {
            float v = y / (float)(size - 1);
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                var color = LerpColor(shadow, baseColor, 0.25f + u * 0.45f + (1f - v) * 0.15f);
                if (y < 4)
                {
                    color = LerpColor(color, highlight, 0.15f);
                }

                if (x > 11)
                {
                    color = LerpColor(color, highlight, 0.25f);
                }

                image[x, y] = color;
            }
        }

        var texture = device.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)size,
            (uint)size,
            mipLevels: 1,
            arrayLayers: 1,
            format: PixelFormat.R8_G8_B8_A8_UNorm_SRgb,
            usage: TextureUsage.Sampled));

        var pixelData = new byte[size * size * 4];
        image.CopyPixelDataTo(pixelData);
        device.UpdateTexture(texture, pixelData, 0, 0, 0, (uint)size, (uint)size, 1u, 0u, 0u);
        image.Dispose();

        var sampler = device.ResourceFactory.CreateSampler(new SamplerDescription
        {
            AddressModeU = SamplerAddressMode.Clamp,
            AddressModeV = SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.MinPoint_MagPoint_MipPoint,
            MaximumAnisotropy = 1
        });

        var regions = new Dictionary<string, AtlasRegion>(StringComparer.OrdinalIgnoreCase)
        {
            ["hand"] = new AtlasRegion(Vector2.Zero, Vector2.One),
            ["missing"] = new AtlasRegion(Vector2.Zero, Vector2.One)
        };

        return new TextureAtlas(texture, sampler, regions, Array.Empty<TextureAtlasAnimation>(), 1);
    }

    private static Rgba32 LerpColor(Rgba32 from, Rgba32 to, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        byte r = (byte)Math.Round(from.R + (to.R - from.R) * amount);
        byte g = (byte)Math.Round(from.G + (to.G - from.G) * amount);
        byte b = (byte)Math.Round(from.B + (to.B - from.B) * amount);
        byte a = (byte)Math.Round(from.A + (to.A - from.A) * amount);
        return new Rgba32(r, g, b, a);
    }

    private static ShaderSetDescription CreateShaders(GraphicsDevice device, float cutoutAlphaThreshold)
    {
        if (device.BackendType == GraphicsBackend.Direct3D11)
        {
            return CreateShadersHlsl(device, cutoutAlphaThreshold);
        }

        string threshold = cutoutAlphaThreshold.ToString("0.###", CultureInfo.InvariantCulture);

        string vertexCode = @"#version 450
layout(set = 0, binding = 0) uniform CameraBuffer
{
    mat4 ViewProjection;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 LocalUV;
layout(location = 2) in vec2 AtlasMin;
layout(location = 3) in vec2 AtlasSize;
layout(location = 4) in vec4 Tint;

layout(location = 0) out vec2 fsin_LocalUV;
layout(location = 1) out vec2 fsin_AtlasMin;
layout(location = 2) out vec2 fsin_AtlasSize;
layout(location = 3) out vec4 fsin_Tint;

void main()
{
    fsin_LocalUV = LocalUV;
    fsin_AtlasMin = AtlasMin;
    fsin_AtlasSize = AtlasSize;
    fsin_Tint = Tint;
    gl_Position = ViewProjection * vec4(Position, 1.0);
}
";

        string fragmentCode = @"#version 450
const float CutoutAlphaThreshold = " + threshold + @";

layout(set = 0, binding = 1) uniform texture2D AtlasTexture;
layout(set = 0, binding = 2) uniform sampler AtlasSampler;

layout(location = 0) in vec2 fsin_LocalUV;
layout(location = 1) in vec2 fsin_AtlasMin;
layout(location = 2) in vec2 fsin_AtlasSize;
layout(location = 3) in vec4 fsin_Tint;

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
    vec2 uv = fsin_AtlasMin + fsin_LocalUV * fsin_AtlasSize;
    vec4 tex = texture(sampler2D(AtlasTexture, AtlasSampler), uv);
    float alpha = tex.a * fsin_Tint.a;
    if (alpha < CutoutAlphaThreshold)
    {
        discard;
    }

    vec3 rgb = tex.rgb * fsin_Tint.rgb;
    rgb = LinearToSrgb(rgb);
    fsout_Color = vec4(rgb, alpha);
}
";

        var factory = device.ResourceFactory;
        var shaders = factory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));

        return new ShaderSetDescription(new[] { FirstPersonVertex.Layout }, shaders);
    }

    private static ShaderSetDescription CreateShadersHlsl(GraphicsDevice device, float cutoutAlphaThreshold)
    {
        string threshold = cutoutAlphaThreshold.ToString("0.###", CultureInfo.InvariantCulture);

        string vertexCode = @"
cbuffer CameraBuffer : register(b0)
{
    row_major float4x4 ViewProjection;
};

struct VSInput
{
    float3 Position  : POSITION0;
    float2 LocalUV   : TEXCOORD0;
    float2 AtlasMin  : TEXCOORD1;
    float2 AtlasSize : TEXCOORD2;
    float4 Tint      : COLOR0;
};

struct VSOutput
{
    float4 Position  : SV_Position;
    float2 LocalUV   : TEXCOORD0;
    float2 AtlasMin  : TEXCOORD1;
    float2 AtlasSize : TEXCOORD2;
    float4 Tint      : COLOR0;
};

VSOutput main(VSInput input)
{
    VSOutput output;
    output.Position = mul(float4(input.Position, 1.0), ViewProjection);
    output.LocalUV = input.LocalUV;
    output.AtlasMin = input.AtlasMin;
    output.AtlasSize = input.AtlasSize;
    output.Tint = input.Tint;
    return output;
}";

        string fragmentCode = @"
const float CutoutAlphaThreshold = " + threshold + @";

Texture2D AtlasTexture : register(t0);
SamplerState AtlasSampler : register(s0);

struct PSInput
{
    float4 Position  : SV_Position;
    float2 LocalUV   : TEXCOORD0;
    float2 AtlasMin  : TEXCOORD1;
    float2 AtlasSize : TEXCOORD2;
    float4 Tint      : COLOR0;
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
    float2 uv = input.AtlasMin + input.LocalUV * input.AtlasSize;
    float4 tex = AtlasTexture.Sample(AtlasSampler, uv);
    float alpha = tex.a * input.Tint.a;
    if (alpha < CutoutAlphaThreshold)
    {
        discard;
    }

    float3 rgb = tex.rgb * input.Tint.rgb;
    rgb = LinearToSrgb(rgb);
    return float4(rgb, alpha);
}";

        var factory = device.ResourceFactory;
        var vertex = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"));
        var fragment = factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));
        return new ShaderSetDescription(new[] { FirstPersonVertex.Layout }, new[] { vertex, fragment });
    }

    private Pipeline CreatePipeline(GraphicsDevice device, ShaderSetDescription shaderSet)
    {
        var pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = DepthStencilStateDescription.Disabled,
            RasterizerState = new RasterizerStateDescription(FaceCullMode.Back, PolygonFillMode.Solid, FrontFace.CounterClockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = new[] { _resourceLayout },
            ShaderSet = shaderSet,
            Outputs = device.MainSwapchain.Framebuffer.OutputDescription
        };

        return device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);
    }

    public void Dispose()
    {
        _pipeline.Dispose();
        _itemResourceSet.Dispose();
        _handResourceSet.Dispose();
        _resourceLayout.Dispose();
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _handAtlas.Dispose();
    }

    private readonly record struct FirstPersonPose(Vector3 Origin, Vector3 Rotation, Vector3 Scale);

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct FirstPersonVertex
    {
        public readonly Vector3 Position;
        public readonly Vector2 LocalUV;
        public readonly Vector2 AtlasMin;
        public readonly Vector2 AtlasSize;
        public readonly Vector4 Tint;

        public FirstPersonVertex(Vector3 position, Vector2 localUV, Vector2 atlasMin, Vector2 atlasSize, Vector4 tint)
        {
            Position = position;
            LocalUV = localUV;
            AtlasMin = atlasMin;
            AtlasSize = atlasSize;
            Tint = tint;
        }

        public static readonly VertexLayoutDescription Layout = new(
            new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float3),
            new VertexElementDescription("LocalUV", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("AtlasMin", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("AtlasSize", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
            new VertexElementDescription("Tint", VertexElementSemantic.Color, VertexElementFormat.Float4));
    }
}
