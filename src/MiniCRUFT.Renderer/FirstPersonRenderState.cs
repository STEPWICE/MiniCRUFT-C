using MiniCRUFT.World;

namespace MiniCRUFT.Renderer;

public readonly struct FirstPersonRenderState
{
    public bool Visible { get; }
    public BlockId HeldBlock { get; }
    public float SwingProgress { get; }
    public float MovementStrength { get; }
    public float WorldTimeSeconds { get; }
    public bool OnGround { get; }

    public FirstPersonRenderState(bool visible, BlockId heldBlock, float swingProgress, float movementStrength, float worldTimeSeconds, bool onGround)
    {
        Visible = visible;
        HeldBlock = heldBlock;
        SwingProgress = swingProgress;
        MovementStrength = movementStrength;
        WorldTimeSeconds = worldTimeSeconds;
        OnGround = onGround;
    }
}
