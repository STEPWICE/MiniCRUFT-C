namespace MiniCRUFT.UI;

public sealed class HudState
{
    public int SelectedSlot { get; set; }
    public int HotbarSize { get; set; } = 9;
    public string DebugText { get; set; } = string.Empty;
    public string MenuText { get; set; } = string.Empty;
    public int Health { get; set; } = 20;
    public int MaxHealth { get; set; } = 20;
}
