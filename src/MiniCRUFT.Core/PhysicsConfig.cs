namespace MiniCRUFT.Core;

public sealed class PhysicsConfig
{
    public float PlayerWidth { get; set; } = 0.6f;
    public float PlayerHeight { get; set; } = 1.8f;
    public float EyeHeight { get; set; } = 1.6f;
    public float Gravity { get; set; } = -20f;
    public float JumpVelocity { get; set; } = 7.5f;
    public float SprintMultiplier { get; set; } = 1.5f;
    public float WaterMoveMultiplier { get; set; } = 0.45f;
    public float WaterGravityMultiplier { get; set; } = 0.2f;
    public float WaterBuoyancy { get; set; } = 3.4f;
    public float WaterJumpVelocity { get; set; } = 4.25f;
    public float WaterMaxFallSpeed { get; set; } = 2.5f;
    public float WaterCurrentMultiplier { get; set; } = 0.9f;
    public int PlayerMaxHealth { get; set; } = 20;
    public float HurtCooldownSeconds { get; set; } = 0.85f;
}
