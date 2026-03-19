namespace MiniCRUFT.UI;

public sealed class HudState
{
    public int SelectedSlot { get; set; }
    public int HotbarSize { get; set; } = 9;
    public float WorldTimeSeconds { get; set; }
    public float PlayerSpeed { get; set; }
    public MiniCRUFT.World.BlockId[] HotbarItems { get; set; } = System.Array.Empty<MiniCRUFT.World.BlockId>();
    public MiniCRUFT.World.BlockId[] InventoryItems { get; set; } = System.Array.Empty<MiniCRUFT.World.BlockId>();
    public int[] HotbarDurability { get; set; } = System.Array.Empty<int>();
    public int[] HotbarMaxDurability { get; set; } = System.Array.Empty<int>();
    public string VersionText { get; set; } = string.Empty;
    public string DebugText { get; set; } = string.Empty;
    public string MenuText { get; set; } = string.Empty;
    public string HerobrineStatusText { get; set; } = string.Empty;
    public string HerobrineToastText { get; set; } = string.Empty;
    public string StatusToastText { get; set; } = string.Empty;
    public string ProgressionMilestonesText { get; set; } = string.Empty;
    public string ProgressionText { get; set; } = string.Empty;
    public string SelectedItemName { get; set; } = string.Empty;
    public bool StrictBetaMode { get; set; }
    public int Health { get; set; } = 20;
    public int MaxHealth { get; set; } = 20;
    public bool SurvivalEnabled { get; set; }
    public int Hunger { get; set; } = 20;
    public int MaxHunger { get; set; } = 20;
    public bool InventoryOpen { get; set; }
    public int InventoryColumns { get; set; } = 9;
    public int InventoryRows { get; set; } = 3;
    public string[] InventoryLabels { get; set; } = System.Array.Empty<string>();
    public int[] HotbarCounts { get; set; } = System.Array.Empty<int>();
}
