namespace MiniCRUFT.Core;

public sealed class ParticleConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxParticles { get; set; } = 256;
    public float Gravity { get; set; } = -4.5f;
    public float Drag { get; set; } = 0.92f;
    public int BlockBreakCount { get; set; } = 12;
    public int BlockPlaceCount { get; set; } = 6;
    public int StepCount { get; set; } = 3;
    public int JumpCount { get; set; } = 4;
    public float BlockBreakLifetime { get; set; } = 0.85f;
    public float BlockPlaceLifetime { get; set; } = 0.45f;
    public float StepLifetime { get; set; } = 0.32f;
    public float JumpLifetime { get; set; } = 0.42f;
    public float BlockBreakSize { get; set; } = 0.16f;
    public float BlockPlaceSize { get; set; } = 0.12f;
    public float StepSize { get; set; } = 0.09f;
    public float JumpSize { get; set; } = 0.1f;
    public float BlockBreakSpeed { get; set; } = 2.8f;
    public float BlockPlaceSpeed { get; set; } = 1.8f;
    public float StepSpeed { get; set; } = 0.8f;
    public float JumpSpeed { get; set; } = 1.6f;
    public float StepUpwardBias { get; set; } = 0.2f;
    public float JumpUpwardBias { get; set; } = 0.35f;
    public int MobHurtCount { get; set; } = 8;
    public int MobDeathCount { get; set; } = 14;
    public float MobHurtLifetime { get; set; } = 0.35f;
    public float MobDeathLifetime { get; set; } = 0.55f;
    public float MobHurtSize { get; set; } = 0.11f;
    public float MobDeathSize { get; set; } = 0.14f;
    public float MobHurtSpeed { get; set; } = 1.6f;
    public float MobDeathSpeed { get; set; } = 2.2f;
    public float MobHurtSpread { get; set; } = 0.18f;
    public float MobDeathSpread { get; set; } = 0.25f;
    public float MobHurtUpwardBias { get; set; } = 0.22f;
    public float MobDeathUpwardBias { get; set; } = 0.3f;
    public float MobHurtMotionInfluence { get; set; } = 0.08f;
    public float MobDeathMotionInfluence { get; set; } = 0.12f;
    public int MobAttackCount { get; set; } = 5;
    public float MobAttackLifetime { get; set; } = 0.24f;
    public float MobAttackSize { get; set; } = 0.1f;
    public float MobAttackSpeed { get; set; } = 1.45f;
    public float MobAttackSpread { get; set; } = 0.16f;
    public float MobAttackUpwardBias { get; set; } = 0.18f;
    public float MobAttackMotionInfluence { get; set; } = 0.07f;
    public float EliteMobParticleMultiplier { get; set; } = 1.35f;
}
