namespace MiniCRUFT.Core;

public sealed class PaletteConfig
{
    public Color3 SandTint { get; set; } = new(0.88f, 0.84f, 0.67f);
    public float SandStrength { get; set; } = 0.35f;
    public Color3 StoneTint { get; set; } = new(0.55f, 0.55f, 0.57f);
    public float StoneStrength { get; set; } = 0.3f;
}
