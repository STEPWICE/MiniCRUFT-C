namespace MiniCRUFT.Core;

public sealed class LodConfig
{
    public bool Enabled { get; set; } = true;
    public float StartDistance { get; set; } = 160f;
    public float EndDistance { get; set; } = 520f;
    public float Step { get; set; } = 8f;
    public float ColorFogBlend { get; set; } = 0.25f;
}
