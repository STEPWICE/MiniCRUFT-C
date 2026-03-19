namespace MiniCRUFT.Core;

public sealed class TntConfig
{
    public bool Enabled { get; set; } = true;
    public bool PrimeOnPlace { get; set; } = true;
    public float FuseSeconds { get; set; } = 4f;
    public float ChainReactionFuseSeconds { get; set; } = 0.35f;
    public float ExplosionRadius { get; set; } = 4.5f;
    public int ExplosionDamage { get; set; } = 20;
    public float KnockbackStrength { get; set; } = 8f;
    public float ResistanceScale { get; set; } = 0.35f;
    public int MaxAffectedBlocks { get; set; } = 512;
    public int MaxPrimedTnt { get; set; } = 4096;
    public int MaxEventQueue { get; set; } = 1024;
}
