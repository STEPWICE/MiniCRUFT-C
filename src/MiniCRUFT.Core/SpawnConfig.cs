using System.Collections.Generic;

namespace MiniCRUFT.Core;

public sealed class SpawnConfig
{
    public string Mode { get; set; } = "AnyNonRiver";
    public int SearchRadius { get; set; } = 2048;
    public int MaxAttempts { get; set; } = 256;
    public int MinHeightAboveSea { get; set; } = 3;
    public float MaxSlope { get; set; } = 9f;
    public bool Randomize { get; set; } = true;
    public List<string> ExcludedBiomes { get; set; } = new() { "River" };
}
