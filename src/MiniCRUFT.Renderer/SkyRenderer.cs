using System;
using System.Numerics;
using System.Text;
using MiniCRUFT.Core;
using Veldrid;
using Veldrid.SPIRV;

namespace MiniCRUFT.Renderer;

public sealed class SkyRenderer : IDisposable
{
    private readonly DeviceBuffer _vertexBuffer;
    private readonly DeviceBuffer _indexBuffer;
    private readonly Pipeline _pipeline;
    private readonly SpriteTexture _sunTexture;
    private readonly SpriteTexture _moonTexture;
    private readonly SpriteBatch _sunBatch;
    private readonly SpriteBatch _moonBatch;
    private readonly bool _moonUsesPhases;

    private readonly SkyVertex[] _quadVertices = new SkyVertex[4];
    private readonly ushort[] _quadIndices = { 0, 1, 2, 0, 2, 3 };

    public SkyRenderer(GraphicsDevice device, AssetStore assets)
    {
        _vertexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(4u * (uint)System.Runtime.InteropServices.Marshal.SizeOf<SkyVertex>(), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _indexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(6u * sizeof(ushort), BufferUsage.IndexBuffer));
        device.UpdateBuffer(_indexBuffer, 0, _quadIndices);

        var shaderSet = CreateShaders(device);
        var pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleDisabled,
            DepthStencilState = DepthStencilStateDescription.Disabled,
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = Array.Empty<ResourceLayout>(),
            ShaderSet = shaderSet,
            Outputs = device.MainSwapchain.Framebuffer.OutputDescription
        };
        _pipeline = device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);

        _sunTexture = LoadSkyTexture(device, assets, "minecraft/textures/environment/sun(without).png", "minecraft/textures/environment/sun.png", repeat: false);
        _moonTexture = LoadSkyTexture(device, assets, "minecraft/textures/environment/moon_phases.png", "minecraft/textures/environment/moon.png", repeat: false);
        _moonUsesPhases = _moonTexture.Size.X > 32f && _moonTexture.Size.Y > 32f;

        _sunBatch = new SpriteBatch(device, _sunTexture.Texture, _sunTexture.Sampler);
        _moonBatch = new SpriteBatch(device, _moonTexture.Texture, _moonTexture.Sampler);
    }

    public void Draw(CommandList commandList, int width, int height, Camera camera, AtmosphereFrame atmosphere, AtmosphereConfig config)
    {
        UpdateGradient(atmosphere.SkyTop, atmosphere.SkyBottom);

        commandList.SetPipeline(_pipeline);
        commandList.SetVertexBuffer(0, _vertexBuffer);
        commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        commandList.UpdateBuffer(_vertexBuffer, 0, _quadVertices);
        commandList.DrawIndexed(6, 1, 0, 0, 0);

        DrawSunMoon(commandList, width, height, camera, atmosphere, config);
    }

    private void DrawSunMoon(CommandList commandList, int width, int height, Camera camera, AtmosphereFrame atmosphere, AtmosphereConfig config)
    {
        float angle = (atmosphere.TimeOfDay - 0.25f) * 2f * MathF.PI;
        var sunRegion = new SpriteRegion(Vector2.Zero, Vector2.One, _sunTexture.Size);
        var sunSize = new Vector2(config.SunSize, config.SunSize);
        float sunAlpha = Math.Clamp((atmosphere.SunIntensity - 0.1f) / 0.35f, 0f, 1f);
        float moonAlpha = Math.Clamp((atmosphere.MoonIntensity - 0.15f) / 0.85f, 0f, 1f) * (1f - atmosphere.RainIntensity * 0.15f);

        if (sunAlpha > 0.001f)
        {
            _sunBatch.Begin(width, height);
            var sunDir = Vector3.Normalize(new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0f));
            var sunPos = camera.Position + sunDir * 1000f;
            if (TryProjectToScreen(camera, sunPos, width, height, out var sunScreen))
            {
                var sunSpritePos = new Vector2(sunScreen.X - sunSize.X * 0.5f, sunScreen.Y - sunSize.Y * 0.5f);
                _sunBatch.Draw(sunRegion, sunSpritePos, sunSize, new Vector4(1f, 1f, 1f, sunAlpha));
            }
            _sunBatch.Flush(commandList);
        }
        if (moonAlpha > 0.001f)
        {
            _moonBatch.Begin(width, height);
            var moonSize = new Vector2(config.MoonSize, config.MoonSize);
            var moonDir = Vector3.Normalize(new Vector3(-MathF.Cos(angle), -MathF.Sin(angle), 0f));
            var moonPos = camera.Position + moonDir * 1000f;
            if (TryProjectToScreen(camera, moonPos, width, height, out var moonScreen))
            {
                var moonSpritePos = new Vector2(moonScreen.X - moonSize.X * 0.5f, moonScreen.Y - moonSize.Y * 0.5f);
                if (_moonUsesPhases)
                {
                    const int phaseColumns = 4;
                    const int phaseRows = 2;
                    const float phaseWidth = 1f / phaseColumns;
                    const float phaseHeight = 1f / phaseRows;
                    int phase = Math.Clamp(atmosphere.MoonPhaseIndex, 0, 7);
                    int phaseX = phase % phaseColumns;
                    int phaseY = phase / phaseColumns;
                    var uvOffset = new Vector2(phaseX * phaseWidth, phaseY * phaseHeight);
                    var uvScale = new Vector2(phaseWidth, phaseHeight);
                    var fullRegion = new SpriteRegion(Vector2.Zero, Vector2.One, _moonTexture.Size);
                    _moonBatch.Draw(fullRegion, moonSpritePos, moonSize, new Vector4(1f, 1f, 1f, moonAlpha), uvOffset, uvScale);
                }
                else
                {
                    var moonRegion = new SpriteRegion(Vector2.Zero, Vector2.One, _moonTexture.Size);
                    _moonBatch.Draw(moonRegion, moonSpritePos, moonSize, new Vector4(1f, 1f, 1f, moonAlpha));
                }
            }
            _moonBatch.Flush(commandList);
        }
    }

    private void UpdateGradient(Vector3 top, Vector3 bottom)
    {
        _quadVertices[0] = new SkyVertex(new Vector2(-1f, -1f), new Vector4(bottom, 1f));
        _quadVertices[1] = new SkyVertex(new Vector2(1f, -1f), new Vector4(bottom, 1f));
        _quadVertices[2] = new SkyVertex(new Vector2(1f, 1f), new Vector4(top, 1f));
        _quadVertices[3] = new SkyVertex(new Vector2(-1f, 1f), new Vector4(top, 1f));
    }

    private static ShaderSetDescription CreateShaders(GraphicsDevice device)
    {
        if (device.BackendType == GraphicsBackend.Direct3D11)
        {
            return CreateShadersHlsl(device);
        }

        const string vertexCode = @"#version 450
layout(location = 0) in vec2 Position;
layout(location = 1) in vec4 Color;
layout(location = 0) out vec4 fsin_Color;
void main()
{
    fsin_Color = Color;
    gl_Position = vec4(Position.xy, 0.0, 1.0);
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
    vec3 rgb = LinearToSrgb(fsin_Color.rgb);
    fsout_Color = vec4(rgb, fsin_Color.a);
}
";

        var factory = device.ResourceFactory;
        var shaders = factory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));

        return new ShaderSetDescription(new[] { SkyVertex.Layout }, shaders);
    }

    private static ShaderSetDescription CreateShadersHlsl(GraphicsDevice device)
    {
        const string vertexCode = @"
struct VSInput
{
    float2 Position : POSITION0;
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
    output.Position = float4(input.Position.xy, 0.0, 1.0);
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
    float3 rgb = LinearToSrgb(input.Color.rgb);
    return float4(rgb, input.Color.a);
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

        return new ShaderSetDescription(new[] { SkyVertex.Layout }, new[] { vertex, fragment });
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _pipeline.Dispose();
        _sunBatch.Dispose();
        _moonBatch.Dispose();
        _sunTexture.Dispose();
        _moonTexture.Dispose();
    }

    private readonly struct SkyVertex
    {
        public readonly Vector2 Position;
        public readonly Vector4 Color;

        public SkyVertex(Vector2 position, Vector4 color)
        {
            Position = position;
            Color = color;
        }

        public static VertexLayoutDescription Layout => new(
            new VertexElementDescription("Position", VertexElementSemantic.Position, VertexElementFormat.Float2),
            new VertexElementDescription("Color", VertexElementSemantic.Color, VertexElementFormat.Float4));
    }

    private static SpriteTexture LoadSkyTexture(GraphicsDevice device, AssetStore assets, string preferredPath, string fallbackPath, bool repeat)
    {
        try
        {
            return SpriteTexture.Load(device, assets, preferredPath, repeat);
        }
        catch
        {
            return SpriteTexture.Load(device, assets, fallbackPath, repeat);
        }
    }

    private static bool TryProjectToScreen(Camera camera, Vector3 worldPos, int width, int height, out Vector2 screenPos)
    {
        var viewProj = camera.View * camera.Projection;
        var clip = Vector4.Transform(new Vector4(worldPos, 1f), viewProj);
        if (clip.W <= 0.001f)
        {
            screenPos = Vector2.Zero;
            return false;
        }

        var ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
        if (ndc.Z < 0f || ndc.Z > 1f)
        {
            screenPos = Vector2.Zero;
            return false;
        }

        float x = (ndc.X * 0.5f + 0.5f) * width;
        float y = (1f - (ndc.Y * 0.5f + 0.5f)) * height;
        screenPos = new Vector2(x, y);
        return true;
    }
}
