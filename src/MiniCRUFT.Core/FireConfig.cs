namespace MiniCRUFT.Core;

public sealed class FireConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxUpdatesPerFrame { get; set; } = 96;
    public int MaxEventQueue { get; set; } = 1024;
    public int MaxExplosionIgnitedBlocks { get; set; } = 32;
    public float MaxAgeSeconds { get; set; } = 8f;
    public float SpreadIntervalSeconds { get; set; } = 0.35f;
    public float SpreadChance { get; set; } = 0.35f;
    public float BurnStartSeconds { get; set; } = 1.25f;
    public float BurnChance { get; set; } = 0.22f;
    public float ExplosionIgniteRadius { get; set; } = 4.5f;
    public float ExplosionIgniteChance { get; set; } = 0.45f;
    public float RainExtinguishMultiplier { get; set; } = 1.75f;
}
