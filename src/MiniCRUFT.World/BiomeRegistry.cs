using System.Collections.Generic;
using System.Numerics;

namespace MiniCRUFT.World;

public static class BiomeRegistry
{
    private static readonly Dictionary<BiomeId, BiomeDefinition> Definitions = new();
    private static readonly object Sync = new();
    private static volatile bool _initialized;
    private static volatile BiomeColorMap? _colorMap;

    public static IReadOnlyDictionary<BiomeId, BiomeDefinition> All => Definitions;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        lock (Sync)
        {
            if (_initialized)
            {
                return;
            }

            Register(new BiomeDefinition(BiomeId.Forest, "Forest", 0.6f, 0.7f, BlockId.Grass));
            Register(new BiomeDefinition(BiomeId.Plains, "Plains", 0.7f, 0.4f, BlockId.Grass));
            Register(new BiomeDefinition(BiomeId.Desert, "Desert", 0.95f, 0.1f, BlockId.Sand));
            Register(new BiomeDefinition(BiomeId.Mountains, "Extreme Hills", 0.2f, 0.4f, BlockId.Stone));
            Register(new BiomeDefinition(BiomeId.River, "River", 0.5f, 0.8f, BlockId.Sand));
            Register(new BiomeDefinition(BiomeId.Taiga, "Taiga", 0.25f, 0.6f, BlockId.Grass));
            Register(new BiomeDefinition(BiomeId.Tundra, "Tundra", 0.1f, 0.3f, BlockId.Snow));
            Register(new BiomeDefinition(BiomeId.Swamp, "Swampland", 0.8f, 0.9f, BlockId.Grass));
            Register(new BiomeDefinition(BiomeId.Savanna, "Savanna", 0.85f, 0.35f, BlockId.Grass));
            Register(new BiomeDefinition(BiomeId.Shrubland, "Shrubland", 0.55f, 0.3f, BlockId.Grass));

            _initialized = true;
        }
    }

    public static void LoadColorMap(MiniCRUFT.Core.AssetStore assets, bool strict = false)
    {
        if (!_initialized)
        {
            Initialize();
        }

        try
        {
            _colorMap = BiomeColorMap.Load(assets, strict);
        }
        catch
        {
            if (strict)
            {
                throw;
            }

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

        if (temperature < 0.18f)
        {
            return BiomeId.Tundra;
        }

        if (temperature < 0.35f && humidity > 0.4f)
        {
            return BiomeId.Taiga;
        }

        if (humidity > 0.78f)
        {
            return BiomeId.Swamp;
        }

        if (temperature > 0.86f && humidity < 0.28f)
        {
            return BiomeId.Desert;
        }

        if (temperature > 0.75f && humidity < 0.5f)
        {
            return BiomeId.Savanna;
        }

        if (humidity > 0.5f)
        {
            return BiomeId.Forest;
        }

        if (humidity < 0.3f)
        {
            return BiomeId.Shrubland;
        }

        return BiomeId.Plains;
    }

    public static BiomeId PickStrictBeta(float temperature, float humidity)
    {
        if (!_initialized)
        {
            Initialize();
        }

        if (temperature > 0.95f && humidity < 0.20f)
        {
            return BiomeId.Desert;
        }

        if (temperature > 0.97f)
        {
            if (humidity > 0.90f)
            {
                return BiomeId.Forest;
            }

            if (humidity >= 0.45f)
            {
                return BiomeId.Forest;
            }

            if (humidity >= 0.20f)
            {
                return BiomeId.Plains;
            }
        }

        if (temperature < 0.70f && humidity > 0.50f)
        {
            return BiomeId.Swamp;
        }

        if (temperature < 0.20f)
        {
            return BiomeId.Tundra;
        }

        if (temperature < 0.50f)
        {
            if (humidity < 0.20f)
            {
                return BiomeId.Tundra;
            }

            return BiomeId.Taiga;
        }

        if (humidity < 0.20f)
        {
            return BiomeId.Savanna;
        }

        if (humidity < 0.35f)
        {
            return BiomeId.Shrubland;
        }

        return BiomeId.Forest;
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
            BiomeId.Taiga => Color(0x5D, 0x8C, 0x5A),
            BiomeId.Tundra => Color(0xB8, 0xC8, 0xB8),
            BiomeId.Swamp => Color(0x3E, 0x7F, 0x3D),
            BiomeId.Savanna => Color(0x8C, 0xB0, 0x4C),
            BiomeId.Shrubland => Color(0x6F, 0xA1, 0x4C),
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
            BiomeId.Taiga => Color(0x4C, 0x7A, 0x4A),
            BiomeId.Tundra => Color(0x9A, 0xB6, 0x9A),
            BiomeId.Swamp => Color(0x2E, 0x6E, 0x2C),
            BiomeId.Savanna => Color(0x7C, 0x9F, 0x45),
            BiomeId.Shrubland => Color(0x5E, 0x92, 0x45),
            _ => Color(0x56, 0xA1, 0x4E)
        };
    }

    private static Vector3 Color(byte r, byte g, byte b)
    {
        return new Vector3(r / 255f, g / 255f, b / 255f);
    }
}
