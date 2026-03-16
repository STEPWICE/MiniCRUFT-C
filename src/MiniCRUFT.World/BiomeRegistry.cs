using System.Collections.Generic;
using System.Numerics;

namespace MiniCRUFT.World;

public static class BiomeRegistry
{
    private static readonly Dictionary<BiomeId, BiomeDefinition> Definitions = new();
    private static bool _initialized;
    private static BiomeColorMap? _colorMap;

    public static IReadOnlyDictionary<BiomeId, BiomeDefinition> All => Definitions;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        Register(new BiomeDefinition(BiomeId.Forest, "forest", 0.6f, 0.7f, BlockId.Grass));
        Register(new BiomeDefinition(BiomeId.Plains, "plains", 0.7f, 0.4f, BlockId.Grass));
        Register(new BiomeDefinition(BiomeId.Desert, "desert", 0.95f, 0.1f, BlockId.Sand));
        Register(new BiomeDefinition(BiomeId.Mountains, "mountains", 0.2f, 0.4f, BlockId.Stone));
        Register(new BiomeDefinition(BiomeId.River, "river", 0.5f, 0.8f, BlockId.Sand));

        _initialized = true;
    }

    public static void LoadColorMap(MiniCRUFT.Core.AssetStore assets)
    {
        if (!_initialized)
        {
            Initialize();
        }

        try
        {
            _colorMap = BiomeColorMap.Load(assets);
        }
        catch
        {
            _colorMap = null;
        }
    }

    private static void Register(BiomeDefinition definition)
    {
        Definitions[definition.Id] = definition;
    }

    public static BiomeDefinition Get(BiomeId id)
    {
        if (!_initialized)
        {
            Initialize();
        }

        return Definitions[id];
    }

    public static BiomeId Pick(float temperature, float humidity)
    {
        if (!_initialized)
        {
            Initialize();
        }

        if (temperature > 0.8f && humidity < 0.35f)
        {
            return BiomeId.Desert;
        }

        if (temperature < 0.35f)
        {
            return BiomeId.Mountains;
        }

        if (humidity > 0.55f)
        {
            return BiomeId.Forest;
        }

        return BiomeId.Plains;
    }

    public static Vector3 GetGrassColor(BiomeId id)
    {
        if (_colorMap != null)
        {
            return _colorMap.GetGrassColor(Get(id));
        }

        return id switch
        {
            BiomeId.Forest => Color(0x4E, 0x8F, 0x3F),
            BiomeId.Plains => Color(0x65, 0xA8, 0x4A),
            BiomeId.Desert => Color(0xB6, 0xC0, 0x6A),
            BiomeId.Mountains => Color(0x7E, 0x8F, 0x7A),
            BiomeId.River => Color(0x5A, 0xAE, 0x4C),
            _ => Color(0x65, 0xA8, 0x4A)
        };
    }

    public static Vector3 GetFoliageColor(BiomeId id)
    {
        if (_colorMap != null)
        {
            return _colorMap.GetFoliageColor(Get(id));
        }

        return id switch
        {
            BiomeId.Forest => Color(0x3E, 0x8A, 0x35),
            BiomeId.Plains => Color(0x56, 0xA1, 0x4E),
            BiomeId.Desert => Color(0x9C, 0xB2, 0x5A),
            BiomeId.Mountains => Color(0x6F, 0x8A, 0x74),
            BiomeId.River => Color(0x4B, 0xA8, 0x45),
            _ => Color(0x56, 0xA1, 0x4E)
        };
    }

    private static Vector3 Color(byte r, byte g, byte b)
    {
        return new Vector3(r / 255f, g / 255f, b / 255f);
    }
}
