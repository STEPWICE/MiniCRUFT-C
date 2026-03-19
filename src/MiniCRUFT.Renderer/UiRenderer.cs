using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using MiniCRUFT.Core;
using MiniCRUFT.UI;
using MiniCRUFT.World;
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

    private readonly FreeTypeFontSystem _fontSystem;
    private readonly HudSpriteAtlas _hudAtlas;
    private readonly ItemIconAtlas _itemAtlas;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteBatch _itemBatch;
    private readonly UiConfig _uiConfig;
    private readonly List<IconDraw> _iconDraws = new();

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

        _fontSystem = new FreeTypeFontSystem(device, assets, _uiConfig);
        _hudAtlas = HudSpriteAtlas.Load(device, assets);
        _spriteBatch = new SpriteBatch(device, _hudAtlas.Atlas.Texture, _hudAtlas.Atlas.Sampler);
        _itemAtlas = ItemIconAtlas.Build(device, assets);
        _itemBatch = new SpriteBatch(device, _itemAtlas.Atlas.Texture, _itemAtlas.Atlas.Sampler);
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
        _iconDraws.Clear();
        // Text is drawn via font sprite batches to avoid backend-specific font texture issues.
        float textScale = ResolveTextScale(height);

        _spriteBatch.Begin(width, height);
        if (!hud.InventoryOpen)
        {
            DrawCrosshair(width, height);
            DrawHotbar(width, height, hud.SelectedSlot, hud.HotbarSize);
            DrawHearts(width, height, hud.Health, hud.MaxHealth);
        }
        _spriteBatch.Flush(commandList);

        if (hud.InventoryOpen)
        {
            DrawInventory(width, height, hud, textScale);
        }
        else
        {
            AddHotbarIcons(width, height, hud, textScale);
            AddHotbarCounts(width, height, hud, textScale);
        }

        if (!hud.InventoryOpen && string.IsNullOrWhiteSpace(hud.MenuText) && !hud.StrictBetaMode && !string.IsNullOrWhiteSpace(hud.SelectedItemName))
        {
            int fontSize = ResolveFontSize(_uiConfig.ItemNameFontSize, textScale, min: 12, max: 28);
            var font = _fontSystem.GetFont(fontSize);
            var size = font.MeasureString(hud.SelectedItemName);
            var (hotbarX, hotbarY, hotbarSize) = GetHotbarRect(width, height);
            float hudScale = ResolveScale(_uiConfig.HudScale, height);
            var heartSize = _hudAtlas.HeartFull.Size * hudScale;
            float heartsTop = height - hotbarSize.Y - _uiConfig.HotbarYOffset * hudScale - heartSize.Y - _uiConfig.HeartsYOffset * hudScale;
            float labelX = hotbarX + (hotbarSize.X - size.X) * 0.5f;
            float labelY = hotbarY - size.Y - _uiConfig.ItemNameYOffset * textScale;
            float heartSafeY = heartsTop - size.Y - _uiConfig.TextMargin * textScale;
            if (labelY > heartSafeY)
            {
                labelY = heartSafeY;
            }
            if (labelY < _uiConfig.TextMargin * textScale)
            {
                labelY = _uiConfig.TextMargin * textScale;
            }
            DrawTextShadow(font, width, height, hud.SelectedItemName, new Vector2(labelX, labelY), Vector4.One, 1f, textScale);
        }

        float margin = _uiConfig.TextMargin * textScale;
        float topTextY = margin;
        if (!string.IsNullOrWhiteSpace(hud.VersionText))
        {
            int fontSize = ResolveFontSize(_uiConfig.DebugFontSize * 0.85f, textScale, min: 9, max: 20);
            var font = _fontSystem.GetFont(fontSize);
            DrawTextShadow(font, width, height, hud.VersionText, new Vector2(margin, topTextY), new Vector4(1f, 1f, 1f, 0.95f), 1f, textScale);
            var versionSize = font.MeasureString(hud.VersionText);
            topTextY += versionSize.Y + margin * 0.35f;
        }

        if (hud.SurvivalEnabled)
        {
            int fontSize = ResolveFontSize(_uiConfig.DebugFontSize * 0.9f, textScale, min: 9, max: 20);
            var font = _fontSystem.GetFont(fontSize);
            string hungerText = $"Hunger: {hud.Hunger}/{hud.MaxHunger}";
            DrawTextShadow(font, width, height, hungerText, new Vector2(margin, topTextY), GetHungerColor(hud.Hunger, hud.MaxHunger), 1f, textScale);
            var hungerSize = font.MeasureString(hungerText);
            topTextY += hungerSize.Y + margin * 0.35f;
        }

        if (!string.IsNullOrWhiteSpace(hud.StatusToastText))
        {
            int fontSize = ResolveFontSize(_uiConfig.DebugFontSize * 0.85f, textScale, min: 9, max: 20);
            var font = _fontSystem.GetFont(fontSize);
            DrawTextShadow(font, width, height, hud.StatusToastText, new Vector2(margin, topTextY), new Vector4(1f, 0.9f, 0.55f, 0.98f), 1f, textScale);
            var toastSize = font.MeasureString(hud.StatusToastText);
            topTextY += toastSize.Y + margin * 0.35f;
        }

        if (!string.IsNullOrWhiteSpace(hud.ProgressionMilestonesText))
        {
            int fontSize = ResolveFontSize(_uiConfig.DebugFontSize * 0.85f, textScale, min: 9, max: 20);
            var font = _fontSystem.GetFont(fontSize);
            DrawTextShadow(font, width, height, hud.ProgressionMilestonesText, new Vector2(margin, topTextY), new Vector4(0.86f, 1f, 0.86f, 0.98f), 1f, textScale);
            var milestoneSize = font.MeasureString(hud.ProgressionMilestonesText);
            topTextY += milestoneSize.Y + margin * 0.35f;
        }

        if (!string.IsNullOrWhiteSpace(hud.ProgressionText))
        {
            int fontSize = ResolveFontSize(_uiConfig.DebugFontSize * 0.9f, textScale, min: 9, max: 20);
            var font = _fontSystem.GetFont(fontSize);
            DrawTextShadow(font, width, height, hud.ProgressionText, new Vector2(margin, topTextY), new Vector4(0.84f, 1f, 0.84f, 0.98f), 1f, textScale);
            var progressionSize = font.MeasureString(hud.ProgressionText);
            topTextY += progressionSize.Y + margin * 0.35f;
        }

        if (string.IsNullOrWhiteSpace(hud.MenuText) && !string.IsNullOrWhiteSpace(hud.DebugText))
        {
            int fontSize = ResolveFontSize(_uiConfig.DebugFontSize, textScale, min: 10, max: 24);
            var font = _fontSystem.GetFont(fontSize);
            DrawTextShadow(font, width, height, hud.DebugText, new Vector2(margin, topTextY), Vector4.One, 1f, textScale);
            var debugSize = font.MeasureString(hud.DebugText);
            topTextY += debugSize.Y + margin * 0.75f;
        }

        if (!string.IsNullOrWhiteSpace(hud.MenuText))
        {
            int fontSize = ResolveFontSize(_uiConfig.MenuFontSize, textScale, min: 12, max: 26);
            var font = _fontSystem.GetFont(fontSize);
            DrawTextShadow(font, width, height, hud.MenuText, new Vector2(margin, topTextY), Vector4.One, 1f, textScale);
        }

        if (!string.IsNullOrWhiteSpace(hud.HerobrineStatusText))
        {
            int fontSize = ResolveFontSize(_uiConfig.MenuFontSize * 0.9f, textScale, min: 10, max: 22);
            var font = _fontSystem.GetFont(fontSize);
            DrawTextPanel(font, hud.HerobrineStatusText, width, height, textScale, alignRight: true, alignCenter: false, new Vector4(0.08f, 0.08f, 0.08f, 0.68f), new Vector4(1f, 1f, 1f, 0.96f));
        }

        if (!string.IsNullOrWhiteSpace(hud.HerobrineToastText))
        {
            int fontSize = ResolveFontSize(_uiConfig.MenuFontSize * 0.95f, textScale, min: 10, max: 22);
            var font = _fontSystem.GetFont(fontSize);
            DrawTextPanel(font, hud.HerobrineToastText, width, height, textScale, alignRight: false, alignCenter: true, new Vector4(0.12f, 0.02f, 0.02f, 0.78f), new Vector4(1f, 0.95f, 0.95f, 0.98f));
        }

        if (_indices.Count > 0)
        {
            commandList.SetPipeline(_pipeline);
            commandList.SetVertexBuffer(0, _vertexBuffer);
            commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            commandList.UpdateBuffer(_vertexBuffer, 0, CollectionsMarshal.AsSpan(_vertices));
            commandList.UpdateBuffer(_indexBuffer, 0, CollectionsMarshal.AsSpan(_indices));
            commandList.DrawIndexed((uint)_indices.Count, 1, 0, 0, 0);
        }

        if (_iconDraws.Count > 0)
        {
            _itemBatch.Begin(width, height);
            foreach (var icon in _iconDraws)
            {
                _itemBatch.Draw(icon.Region, icon.Position, icon.Size, icon.Color);
            }
            _itemBatch.Flush(commandList);
        }

        _fontSystem.Flush(commandList);
    }

    private void DrawInventory(int width, int height, HudState hud, float textScale)
    {
        float scale = ResolveScale(_uiConfig.InventoryScale, height);
        float slot = _uiConfig.InventorySlotSize * scale;
        float padding = _uiConfig.InventorySlotPadding * scale;
        float panelPadding = _uiConfig.InventoryPanelPadding * scale;
        float border = MathF.Max(2f, 2f * scale);
        float gap = padding * 2f;

        int cols = Math.Max(1, hud.InventoryColumns);
        int rows = Math.Max(1, hud.InventoryRows);
        int hotbarSlots = Math.Max(1, hud.HotbarSize);

        float gridWidth = cols * slot + (cols - 1) * padding;
        float gridHeight = rows * slot + (rows - 1) * padding;
        float hotbarWidth = hotbarSlots * slot + (hotbarSlots - 1) * padding;

        int titleFontSize = ResolveFontSize(_uiConfig.InventoryTitleFontSize, textScale, min: 12, max: 28);
        var titleFont = _fontSystem.GetFont(titleFontSize);
        var titleSize = titleFont.MeasureString("Inventory");

        float panelWidth = MathF.Max(gridWidth, hotbarWidth) + panelPadding * 2f;
        float panelHeight = panelPadding * 2f + titleSize.Y + gap + gridHeight + gap + slot;

        float panelX = width * 0.5f - panelWidth * 0.5f;
        float panelY = height * 0.5f - panelHeight * 0.5f;

        DrawRect(0, 0, width, height, new Vector4(0f, 0f, 0f, 0.45f), width, height);
        DrawRect(panelX - border, panelY - border, panelWidth + border * 2f, panelHeight + border * 2f, new Vector4(0f, 0f, 0f, 0.85f), width, height);
        DrawRect(panelX, panelY, panelWidth, panelHeight, new Vector4(0.12f, 0.12f, 0.12f, 0.85f), width, height);

        float titleX = panelX + panelPadding;
        float titleY = panelY + panelPadding * 0.5f;
        DrawTextShadow(titleFont, width, height, "Inventory", new Vector2(titleX, titleY), Vector4.One, 1f, textScale);

        float gridStartX = panelX + panelPadding;
        float gridStartY = panelY + panelPadding + titleSize.Y + gap * 0.5f;

        DrawSlotGrid(gridStartX, gridStartY, cols, rows, slot, padding, width, height, scale, hud.InventoryLabels, 0, selectedIndex: -1);
        AddSlotIcons(gridStartX, gridStartY, cols, rows, slot, padding, hud.InventoryItems, 0);

        float hotbarX = panelX + panelPadding + (panelWidth - panelPadding * 2f - hotbarWidth) * 0.5f;
        float hotbarY = gridStartY + gridHeight + gap;
        int hotbarStartIndex = rows * cols;
        DrawSlotGrid(hotbarX, hotbarY, hotbarSlots, 1, slot, padding, width, height, scale, hud.InventoryLabels, hotbarStartIndex, hud.SelectedSlot);
        AddSlotIcons(hotbarX, hotbarY, hotbarSlots, 1, slot, padding, hud.InventoryItems, hotbarStartIndex);
        DrawItemValueOverlays(width, height, hud, textScale, hotbarX, hotbarY, slot, slot, slot + padding, hotbarSlots);
    }

    private void DrawSlotGrid(float startX, float startY, int cols, int rows, float slot, float padding, int width, int height, float scale, string[] labels, int startIndex, int selectedIndex)
    {
        float border = MathF.Max(2f, slot * 0.1f);
        int labelFontSize = Math.Max(8, (int)MathF.Round(10f * scale));
        var labelFont = _fontSystem.GetFont(labelFontSize);

        int slotIndex = startIndex;
        for (int row = 0; row < rows; row++)
        {
            float y = startY + row * (slot + padding);
            for (int col = 0; col < cols; col++)
            {
                float x = startX + col * (slot + padding);
                bool selected = selectedIndex == col && rows == 1;
                var borderColor = selected ? new Vector4(1f, 1f, 1f, 0.9f) : new Vector4(0f, 0f, 0f, 0.85f);
                DrawRect(x - border, y - border, slot + border * 2f, slot + border * 2f, borderColor, width, height);
                DrawRect(x, y, slot, slot, new Vector4(0.18f, 0.18f, 0.18f, 0.95f), width, height);

                if (_uiConfig.ShowSlotLabels && slotIndex >= 0 && slotIndex < labels.Length)
                {
                    var label = labels[slotIndex];
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        var textSize = labelFont.MeasureString(label);
                        float maxWidth = slot * 0.8f;
                        float maxHeight = slot * 0.8f;
                        float scaleX = textSize.X > 0f ? maxWidth / textSize.X : 1f;
                        float scaleY = textSize.Y > 0f ? maxHeight / textSize.Y : 1f;
                        float textScale = MathF.Min(1f, MathF.Min(scaleX, scaleY));
                        var scaledSize = textSize * textScale;
                        float tx = x + slot * 0.5f - scaledSize.X * 0.5f;
                        float ty = y + slot * 0.5f - scaledSize.Y * 0.5f;
                        labelFont.DrawText(width, height, label, new Vector2(tx, ty), Vector4.One, textScale);
                    }
                }
                slotIndex++;
            }
        }
    }

    private void AddHotbarIcons(int width, int height, HudState hud, float textScale)
    {
        if (hud.HotbarItems.Length == 0 || hud.HotbarSize <= 0)
        {
            return;
        }

        var (hotbarX, hotbarY, hotbarSize) = GetHotbarRect(width, height);
        float slotWidth = hotbarSize.X / hud.HotbarSize;
        float slotHeight = hotbarSize.Y;
        float iconSize = MathF.Min(slotWidth, slotHeight) * _uiConfig.ItemIconScale;
        float iconYOffset = (slotHeight - iconSize) * 0.5f;

        for (int i = 0; i < hud.HotbarSize && i < hud.HotbarItems.Length; i++)
        {
            var item = hud.HotbarItems[i];
            if (item == BlockId.Air)
            {
                continue;
            }

            float x = hotbarX + i * slotWidth + (slotWidth - iconSize) * 0.5f;
            float y = hotbarY + iconYOffset;
            _iconDraws.Add(new IconDraw(_itemAtlas.GetRegion(item), new Vector2(x, y), new Vector2(iconSize, iconSize), Vector4.One));
        }
    }

    private void AddHotbarCounts(int width, int height, HudState hud, float textScale)
    {
        var (hotbarX, hotbarY, hotbarSize) = GetHotbarRect(width, height);
        float slotWidth = hotbarSize.X / Math.Max(1, hud.HotbarSize);
        DrawItemValueOverlays(width, height, hud, textScale, hotbarX, hotbarY, slotWidth, hotbarSize.Y, slotWidth, hud.HotbarSize);
    }

    private void DrawItemValueOverlays(int width, int height, HudState hud, float textScale, float startX, float startY, float slotWidth, float slotHeight, float slotStride, int slotCount)
    {
        if (hud.HotbarItems.Length == 0 || slotCount <= 0)
        {
            return;
        }

        float insetX = MathF.Max(2f, _uiConfig.HotbarCountMarginX * textScale);
        float insetY = MathF.Max(2f, _uiConfig.HotbarCountMarginY * textScale);
        int fontSize = ResolveFontSize(_uiConfig.HotbarCountFontSize, textScale, min: 8, max: 18);
        var font = _fontSystem.GetFont(fontSize);

        for (int i = 0; i < slotCount && i < hud.HotbarItems.Length && i < hud.HotbarCounts.Length; i++)
        {
            var item = hud.HotbarItems[i];
            int count = hud.HotbarCounts[i];
            int maxDurability = i < hud.HotbarMaxDurability.Length ? hud.HotbarMaxDurability[i] : 0;
            int durability = i < hud.HotbarDurability.Length ? hud.HotbarDurability[i] : 0;
            if (item == BlockId.Air)
            {
                continue;
            }

            string text;
            Vector4 color = Vector4.One;
            if (maxDurability > 0)
            {
                int clampedDurability = Math.Clamp(durability, 0, maxDurability);
                float ratio = clampedDurability / (float)maxDurability;
                int percent = (int)MathF.Round(ratio * 100f);
                text = percent.ToString(CultureInfo.InvariantCulture) + "%";
                color = GetDurabilityColor(ratio);
            }
            else if (count > 1)
            {
                text = count.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                continue;
            }

            var size = font.MeasureString(text);
            float x = startX + i * slotStride + slotWidth - size.X - insetX;
            float y = startY + slotHeight - size.Y - insetY;
            DrawTextShadow(font, width, height, text, new Vector2(x, y), color, 1f, textScale);
        }
    }

    private void AddSlotIcons(float startX, float startY, int cols, int rows, float slot, float padding, BlockId[] items, int startIndex)
    {
        if (items.Length == 0)
        {
            return;
        }

        float iconSize = slot * _uiConfig.ItemIconScale;
        float iconOffset = (slot - iconSize) * 0.5f;
        int index = startIndex;

        for (int row = 0; row < rows; row++)
        {
            float y = startY + row * (slot + padding);
            for (int col = 0; col < cols; col++)
            {
                if (index >= items.Length)
                {
                    return;
                }

                var item = items[index];
                if (item != BlockId.Air)
                {
                    float x = startX + col * (slot + padding);
                    _iconDraws.Add(new IconDraw(_itemAtlas.GetRegion(item), new Vector2(x + iconOffset, y + iconOffset), new Vector2(iconSize, iconSize), Vector4.One));
                }

                index++;
            }
        }
    }

    private void DrawCrosshair(int width, int height)
    {
        var region = _hudAtlas.Crosshair;
        float scale = ResolveScale(_uiConfig.HudScale, height);
        var size = region.Size * scale;
        var pos = new Vector2(width / 2f - size.X * 0.5f, height / 2f - size.Y * 0.5f);
        _spriteBatch.Draw(region, pos, size, Vector4.One);
    }

    private void DrawHotbar(int width, int height, int selected, int slots)
    {
        float scale = ResolveScale(_uiConfig.HudScale, height);
        var bar = _hudAtlas.Hotbar;
        var selection = _hudAtlas.HotbarSelection;
        var barSize = bar.Size * scale;
        float x = width / 2f - barSize.X / 2f;
        float y = height - barSize.Y - _uiConfig.HotbarYOffset * scale;
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
        float scale = ResolveScale(_uiConfig.HudScale, height);
        var heartSize = _hudAtlas.HeartFull.Size * scale;

        var barSize = _hudAtlas.Hotbar.Size * scale;
        float baseX = width / 2f - barSize.X / 2f + _uiConfig.HeartsXOffset * scale;
        float baseY = height - barSize.Y - _uiConfig.HotbarYOffset * scale - heartSize.Y - _uiConfig.HeartsYOffset * scale;

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

    private static Vector4 GetHungerColor(int hunger, int maxHunger)
    {
        if (maxHunger <= 0)
        {
            return new Vector4(1f, 1f, 1f, 0.95f);
        }

        float ratio = Math.Clamp(hunger / (float)maxHunger, 0f, 1f);
        if (ratio <= 0.2f)
        {
            return new Vector4(1f, 0.45f, 0.45f, 0.98f);
        }

        if (ratio <= 0.5f)
        {
            return new Vector4(1f, 0.88f, 0.45f, 0.98f);
        }

        return new Vector4(0.92f, 1f, 0.92f, 0.96f);
    }

    private static Vector4 GetDurabilityColor(float ratio)
    {
        if (ratio <= 0.2f)
        {
            return new Vector4(1f, 0.45f, 0.45f, 0.98f);
        }

        if (ratio <= 0.5f)
        {
            return new Vector4(1f, 0.88f, 0.45f, 0.98f);
        }

        return new Vector4(0.92f, 1f, 0.92f, 0.98f);
    }

    private float ResolveScale(float baseScale, int height)
    {
        if (!_uiConfig.AutoScale || _uiConfig.ReferenceHeight <= 0f)
        {
            return baseScale;
        }

        return baseScale * (height / _uiConfig.ReferenceHeight);
    }

    private float ResolveTextScale(int height)
    {
        if (!_uiConfig.AutoScale || _uiConfig.ReferenceHeight <= 0f)
        {
            return 1f;
        }

        return height / _uiConfig.ReferenceHeight;
    }

    private static int ResolveFontSize(float baseSize, float scale, int min, int max)
    {
        int size = (int)MathF.Round(baseSize * scale);
        if (size < min)
        {
            return min;
        }

        if (size > max)
        {
            return max;
        }

        return size;
    }

    private void DrawTextShadow(FreeTypeFont font, int width, int height, string text, Vector2 pos, Vector4 color, float scale, float textScale)
    {
        DrawTextShadow(font, width, height, text.AsSpan(), pos, color, scale, textScale);
    }

    private void DrawTextShadow(FreeTypeFont font, int width, int height, ReadOnlySpan<char> text, Vector2 pos, Vector4 color, float scale, float textScale)
    {
        float offset = MathF.Max(1f, MathF.Round(_uiConfig.TextShadowOffset * textScale));
        var shadowColor = new Vector4(0f, 0f, 0f, _uiConfig.TextShadowAlpha);
        font.DrawText(width, height, text, pos + new Vector2(offset, offset), shadowColor, scale);
        font.DrawText(width, height, text, pos, color, scale);
    }

    private void DrawTextPanel(FreeTypeFont font, string text, int width, int height, float textScale, bool alignRight, bool alignCenter, Vector4 backgroundColor, Vector4 textColor)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        ReadOnlySpan<char> span = text.AsSpan();
        float margin = _uiConfig.TextMargin * textScale;
        float padding = MathF.Max(4f, margin * 0.75f);
        float lineGap = MathF.Max(1f, font.LineHeight * 0.12f);
        float panelWidth = 0f;
        float panelHeight = 0f;
        int lineCount = 0;

        for (int lineStart = 0; lineStart < span.Length;)
        {
            int lineEnd = lineStart;
            while (lineEnd < span.Length && span[lineEnd] != '\n')
            {
                lineEnd++;
            }

            int trimmedEnd = lineEnd;
            while (trimmedEnd > lineStart && span[trimmedEnd - 1] == '\r')
            {
                trimmedEnd--;
            }

            if (trimmedEnd > lineStart)
            {
                var line = span.Slice(lineStart, trimmedEnd - lineStart);
                Vector2 size = font.MeasureString(line);
                panelWidth = MathF.Max(panelWidth, size.X);
                panelHeight += size.Y;
                if (lineCount > 0)
                {
                    panelHeight += lineGap;
                }
                lineCount++;
            }

            if (lineEnd >= span.Length)
            {
                break;
            }

            lineStart = lineEnd + 1;
        }

        if (lineCount == 0)
        {
            return;
        }

        panelWidth += padding * 2f;
        panelHeight += padding * 2f;

        float x = alignCenter
            ? width * 0.5f - panelWidth * 0.5f
            : alignRight
                ? width - margin - panelWidth
                : margin;
        float y = margin;

        DrawRect(x, y, panelWidth, panelHeight, backgroundColor, width, height);

        float lineY = y + padding;
        for (int lineStart = 0; lineStart < span.Length;)
        {
            int lineEnd = lineStart;
            while (lineEnd < span.Length && span[lineEnd] != '\n')
            {
                lineEnd++;
            }

            int trimmedEnd = lineEnd;
            while (trimmedEnd > lineStart && span[trimmedEnd - 1] == '\r')
            {
                trimmedEnd--;
            }

            if (trimmedEnd > lineStart)
            {
                var line = span.Slice(lineStart, trimmedEnd - lineStart);
                Vector2 size = font.MeasureString(line);
                float lineX = alignCenter
                    ? x + (panelWidth - size.X) * 0.5f
                    : x + padding;
                DrawTextShadow(font, width, height, line, new Vector2(lineX, lineY), textColor, 1f, textScale);
                lineY += size.Y + lineGap;
            }

            if (lineEnd >= span.Length)
            {
                break;
            }

            lineStart = lineEnd + 1;
        }
    }

    private (float x, float y, Vector2 size) GetHotbarRect(int width, int height)
    {
        float scale = ResolveScale(_uiConfig.HudScale, height);
        var bar = _hudAtlas.Hotbar;
        var barSize = bar.Size * scale;
        float x = width / 2f - barSize.X / 2f;
        float y = height - barSize.Y - _uiConfig.HotbarYOffset * scale;
        return (x, y, barSize);
    }

    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _pipeline.Dispose();
        _fontSystem.Dispose();
        _spriteBatch.Dispose();
        _itemBatch.Dispose();
        _hudAtlas.Atlas.Dispose();
        _itemAtlas.Dispose();
    }

    private readonly struct IconDraw
    {
        public SpriteRegion Region { get; }
        public Vector2 Position { get; }
        public Vector2 Size { get; }
        public Vector4 Color { get; }

        public IconDraw(SpriteRegion region, Vector2 position, Vector2 size, Vector4 color)
        {
            Region = region;
            Position = position;
            Size = size;
            Color = color;
        }
    }
}
