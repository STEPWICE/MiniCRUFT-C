namespace MiniCRUFT.Core;

public sealed class PhysicsConfig
{
    public float PlayerWidth { get; set; } = 0.6f;
    public float PlayerHeight { get; set; } = 1.8f;
    public float EyeHeight { get; set; } = 1.6f;
    public float Gravity { get; set; } = -20f;
    public float JumpVelocity { get; set; } = 7.5f;
    public float SprintMultiplier { get; set; } = 1.5f;
}
