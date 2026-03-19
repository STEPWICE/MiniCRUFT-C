using System.Collections.Generic;
using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public static class FoodCatalog
{
    private static readonly Dictionary<BlockId, int> FoodValues = new()
    {
        [BlockId.Bread] = 5,
        [BlockId.Apple] = 4,
        [BlockId.RawBeef] = 3,
        [BlockId.CookedBeef] = 8,
        [BlockId.RawChicken] = 2,
        [BlockId.CookedChicken] = 6,
        [BlockId.RawMutton] = 2,
        [BlockId.CookedMutton] = 6
    };

    public static bool IsFood(BlockId item)
    {
        return FoodValues.ContainsKey(item);
    }

    public static bool TryGetHungerRestore(BlockId item, out int hungerRestore)
    {
        return FoodValues.TryGetValue(item, out hungerRestore);
    }
}
