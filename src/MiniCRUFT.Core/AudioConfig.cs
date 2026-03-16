namespace MiniCRUFT.Core;

public sealed class AudioConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxActive { get; set; } = 16;
    public float DigVolume { get; set; } = 0.85f;
    public float PlaceVolume { get; set; } = 0.6f;
}
