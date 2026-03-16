using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using FontStashSharp;
using MiniCRUFT.Core;
using MiniCRUFT.UI;
using Veldrid;
using Veldrid.SPIRV;

namespace MiniCRUFT.Renderer;

public sealed class UiRenderer : IDisposable
{
    private readonly DeviceBuffer _vertexBuffer;
    private readonly DeviceBuffer _indexBuffer;
    private readonly Pipeline _pipeline;

    private readonly List<UiVertex> _vertices = new();
    private readonly List<ushort> _indices = new();

    private readonly UiTextRenderer _textRenderer;
    private readonly FontSystem _fontSystem;
    private readonly FontStashRenderer _fontRenderer;
    private readonly HudSpriteAtlas _hudAtlas;
    private readonly SpriteBatch _spriteBatch;
    private readonly UiConfig _uiConfig;

    public UiRenderer(GraphicsDevice device, AssetStore assets, UiConfig uiConfig)
    {
        _uiConfig = uiConfig;
        _vertexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(1024 * 1024, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _indexBuffer = device.ResourceFactory.CreateBuffer(new BufferDescription(256 * 1024, BufferUsage.IndexBuffer | BufferUsage.Dynamic));

        var shaders = CreateShaders(device);

        var pipelineDescription = new GraphicsPipelineDescription
        {
            BlendState = BlendStateDescription.SingleAlphaBlend,
            DepthStencilState = DepthStencilStateDescription.Disabled,
            RasterizerState = new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, true, false),
            PrimitiveTopology = PrimitiveTopology.TriangleList,
            ResourceLayouts = Array.Empty<ResourceLayout>(),
            ShaderSet = shaders,
            Outputs = device.MainSwapchain.Framebuffer.OutputDescription
        };

        _pipeline = device.ResourceFactory.CreateGraphicsPipeline(pipelineDescription);

        _textRenderer = new UiTextRenderer(device);
        _fontSystem = new FontSystem(new FontSystemSettings
        {
            FontResolutionFactor = 2f,
            KernelWidth = 2,
            KernelHeight = 2
        });

        bool notoLoaded = false;
        try
        {
            var fontBytes = assets.ReadAllBytes("minecraft/font/NotoSans-Regular.ttf");
            _fontSystem.AddFont(fontBytes);
            notoLoaded = true;
            Log.Info("UI font loaded: NotoSans-Regular.ttf");
        }
        catch (Exception ex)
        {
            Log.Warn($"UI font load failed (NotoSans-Regular.ttf): {ex.Message}");
        }

        try
        {
            var fallbackBytes = assets.ReadAllBytes("minecraft/font/consolas.ttf");
            _fontSystem.AddFont(fallbackBytes);
            Log.Info("UI font loaded: consolas.ttf");
        }
        catch (Exception ex)
        {
            if (!notoLoaded)
            {
                Log.Warn($"UI font load failed (consolas.ttf): {ex.Message}");
            }
        }

        _fontRenderer = new FontStashRenderer(_textRenderer, device);
        _hudAtlas = HudSpriteAtlas.Load(device, assets);
        _spriteBatch = new SpriteBatch(device, _hudAtlas.Atlas.Texture, _hudAtlas.Atlas.Sampler);
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

void main()
{
    fsout_Color = fsin_Color;
}
";

        var factory = device.ResourceFactory;
        var shaders = factory.CreateFromSpirv(
            new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"),
            new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main"));

        return new ShaderSetDescription(new[] { UiVertex.Layout }, shaders);
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

float4 main(PSInput input) : SV_Target
{
    return input.Color;
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

        return new ShaderSetDescription(new[] { UiVertex.Layout }, new[] { vertex, fragment });
    }

    public void Draw(CommandList commandList, OutputDescription outputDescription, HudState hud, int width, int height)
    {
        _vertices.Clear();
        _indices.Clear();
        _textRenderer.Begin(width, height);

        _spriteBatch.Begin(width, height);
        DrawCrosshair(width, height);
        DrawHotbar(width, height, hud.SelectedSlot, hud.HotbarSize);
        DrawHearts(width, height, hud.Health, hud.MaxHealth);
        _spriteBatch.Flush(commandList);

        if (!string.IsNullOrWhiteSpace(hud.DebugText))
        {
            var font = _fontSystem.GetFont(16);
            font.DrawText(_fontRenderer, hud.DebugText, new Vector2(12, 12), FSColor.White, 1f, Vector2.Zero, null, 0f, 0f, 0f, TextStyle.None, FontSystemEffect.None, 0);
        }

        if (!string.IsNullOrWhiteSpace(hud.MenuText))
        {
            var font = _fontSystem.GetFont(18);
            font.DrawText(_fontRenderer, hud.MenuText, new Vector2(12, 80), FSColor.White, 1f, Vector2.Zero, null, 0f, 0f, 0f, TextStyle.None, FontSystemEffect.None, 0);
        }

        if (_indices.Count > 0)
        {
            commandList.SetPipeline(_pipeline);
            commandList.SetVertexBuffer(0, _vertexBuffer);
            commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            commandList.UpdateBuffer(_vertexBuffer, 0, _vertices.ToArray());
            commandList.UpdateBuffer(_indexBuffer, 0, _indices.ToArray());
            commandList.DrawIndexed((uint)_indices.Count, 1, 0, 0, 0);
        }

        _textRenderer.Flush(commandList);
    }

    private void DrawCrosshair(int width, int height)
    {
        var region = _hudAtlas.Crosshair;
        float scale = _uiConfig.HudScale;
        var size = region.Size * scale;
        var pos = new Vector2(width / 2f - size.X * 0.5f, height / 2f - size.Y * 0.5f);
        _spriteBatch.Draw(region, pos, size, Vector4.One);
    }

    private void DrawHotbar(int width, int height, int selected, int slots)
    {
        float scale = _uiConfig.HudScale;
        var bar = _hudAtlas.Hotbar;
        var selection = _hudAtlas.HotbarSelection;
        var barSize = bar.Size * scale;
        float x = width / 2f - barSize.X / 2f;
        float y = height - barSize.Y - _uiConfig.HotbarYOffset;

        _spriteBatch.Draw(bar, new Vector2(x, y), barSize, Vector4.One);

        float slotWidth = barSize.X / slots;
        var selSize = selection.Size * scale;
        float selX = x + selected * slotWidth + (slotWidth - selSize.X) * 0.5f;
        float selY = y + (barSize.Y - selSize.Y) * 0.5f;
        _spriteBatch.Draw(selection, new Vector2(selX, selY), selSize, Vector4.One);
    }

    private void DrawHearts(int width, int height, int health, int maxHealth)
    {
        int hearts = Math.Max(1, maxHealth / 2);
        float scale = _uiConfig.HudScale;
        var heartSize = _hudAtlas.HeartFull.Size * scale;

        var barSize = _hudAtlas.Hotbar.Size * scale;
        float baseX = width / 2f - barSize.X / 2f + _uiConfig.HeartsXOffset;
        float baseY = height - barSize.Y - _uiConfig.HotbarYOffset - heartSize.Y - _uiConfig.HeartsYOffset;

        for (int i = 0; i < hearts; i++)
        {
            float x = baseX + i * (heartSize.X + 2f);
            float y = baseY;

            int hp = health - i * 2;
            SpriteRegion region = hp >= 2 ? _hudAtlas.HeartFull : hp == 1 ? _hudAtlas.HeartHalf : _hudAtlas.HeartEmpty;
            _spriteBatch.Draw(region, new Vector2(x, y), heartSize, Vector4.One);
        }
    }

    private void DrawRect(float x, float y, float w, float h, Vector4 color, int width, int height)
    {
        var p0 = ToNdc(x, y, width, height);
        var p1 = ToNdc(x + w, y, width, height);
        var p2 = ToNdc(x + w, y + h, width, height);
        var p3 = ToNdc(x, y + h, width, height);

        ushort start = (ushort)_vertices.Count;
        _vertices.Add(new UiVertex(p0, color));
        _vertices.Add(new UiVertex(p1, color));
        _vertices.Add(new UiVertex(p2, color));
        _vertices.Add(new UiVertex(p3, color));

        _indices.Add(start);
        _indices.Add((ushort)(start + 1));
        _indices.Add((ushort)(start + 2));
        _indices.Add(start);
        _indices.Add((ushort)(start + 2));
        _indices.Add((ushort)(start + 3));
    }

    private static Vector2 ToNdc(float x, float y, int width, int height)
    {
        float ndcX = (x / width) * 2f - 1f;
        float ndcY = 1f - (y / height) * 2f;
        return new Vector2(ndcX, ndcY);
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _pipeline.Dispose();
        _textRenderer.Dispose();
        _spriteBatch.Dispose();
        _hudAtlas.Atlas.Dispose();
    }
}
