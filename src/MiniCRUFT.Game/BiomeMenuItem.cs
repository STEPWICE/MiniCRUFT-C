using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public readonly struct BiomeMenuItem
{
    public string Label { get; }
    public BiomeId? Biome { get; }

    public BiomeMenuItem(string label, BiomeId? biome)
    {
        Label = label;
        Biome = biome;
    }
}
