namespace MiniCRUFT.Core;

public sealed class UiConfig
{
    public float HudScale { get; set; } = 2.0f;
    public float HotbarYOffset { get; set; } = 16f;
    public float HeartsYOffset { get; set; } = 6f;
    public float HeartsXOffset { get; set; } = 0f;
    public float InventoryScale { get; set; } = 2.0f;
    public float InventorySlotSize { get; set; } = 20f;
    public float InventorySlotPadding { get; set; } = 4f;
    public float InventoryPanelPadding { get; set; } = 12f;
    public int InventoryColumns { get; set; } = 9;
    public int InventoryRows { get; set; } = 3;
    public bool ShowSlotLabels { get; set; } = false;
    public float DebugFontSize { get; set; } = 14f;
    public float MenuFontSize { get; set; } = 16f;
    public float InventoryTitleFontSize { get; set; } = 18f;
    public float ItemNameFontSize { get; set; } = 16f;
    public float ItemNameYOffset { get; set; } = 10f;
    public float ItemNameDisplaySeconds { get; set; } = 1.6f;
    public float ItemIconScale { get; set; } = 0.85f;
    public float HotbarCountFontSize { get; set; } = 12f;
    public float HotbarCountMarginX { get; set; } = 4f;
    public float HotbarCountMarginY { get; set; } = 3f;
    public float TextMargin { get; set; } = 8f;
    public float TextShadowAlpha { get; set; } = 0.85f;
    public float TextShadowOffset { get; set; } = 1f;
    public bool AutoScale { get; set; } = true;
    public float ReferenceHeight { get; set; } = 1080f;
    public string FontFile { get; set; } = "minecraft/font/NotoSans-Regular.ttf";
    public bool FontInvertMask { get; set; } = false;
}
