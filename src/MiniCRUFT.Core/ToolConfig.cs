namespace MiniCRUFT.Core;

public sealed class ToolConfig
{
    public int WoodMaxDurability { get; set; } = 64;
    public int StoneMaxDurability { get; set; } = 128;
    public int IronMaxDurability { get; set; } = 256;
    public int DiamondMaxDurability { get; set; } = 512;
    public int ToolWearPerAction { get; set; } = 1;
    public float BlockBreakTimeScale { get; set; } = 0.5f;
    public float MinBreakSeconds { get; set; } = 0.08f;
    public float WrongToolBreakPenalty { get; set; } = 1.6f;
    public float WoodMiningSpeedMultiplier { get; set; } = 1f;
    public float StoneMiningSpeedMultiplier { get; set; } = 1.35f;
    public float IronMiningSpeedMultiplier { get; set; } = 1.8f;
    public float DiamondMiningSpeedMultiplier { get; set; } = 2.6f;
    public int WoodRepairDurability { get; set; } = 16;
    public int StoneRepairDurability { get; set; } = 32;
    public int IronRepairDurability { get; set; } = 64;
    public int DiamondRepairDurability { get; set; } = 128;
}
