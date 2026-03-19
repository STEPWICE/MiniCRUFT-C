namespace MiniCRUFT.Core;

public sealed class FallingConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxUpdatesPerFrame { get; set; } = 64;
}
