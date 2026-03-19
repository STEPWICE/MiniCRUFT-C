using System;

namespace MiniCRUFT.World;

public static class PoiLootClassifier
{
    public static PoiLootKind Classify(Chunk chunk, int localX, int localY, int localZ, int seaLevel)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        int campMarkers = 0;
        int supportBlocks = 0;
        int cobblestoneBlocks = 0;
        int gravelBlocks = 0;
        int torchBlocks = 0;

        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dz = -2; dz <= 2; dz++)
                {
                    if (dx == 0 && dy == 0 && dz == 0)
                    {
                        continue;
                    }

                    BlockId block = chunk.GetBlock(localX + dx, localY + dy, localZ + dz);
                    switch (block)
                    {
                        case BlockId.Furnace:
                        case BlockId.CraftingTable:
                            campMarkers++;
                            break;
                        case BlockId.Wood:
                        case BlockId.Planks:
                        case BlockId.BirchWood:
                        case BlockId.SpruceWood:
                            supportBlocks++;
                            break;
                        case BlockId.Cobblestone:
                            cobblestoneBlocks++;
                            break;
                        case BlockId.Gravel:
                            gravelBlocks++;
                            break;
                        case BlockId.Torch:
                        case BlockId.TorchWallNorth:
                        case BlockId.TorchWallSouth:
                        case BlockId.TorchWallWest:
                        case BlockId.TorchWallEast:
                            torchBlocks++;
                            break;
                    }
                }
            }
        }

        if (campMarkers > 0)
        {
            return PoiLootKind.Camp;
        }

        if (supportBlocks >= 7 && torchBlocks >= 2 && localY <= seaLevel - 4)
        {
            return PoiLootKind.MineShaft;
        }

        if (cobblestoneBlocks >= 6 && torchBlocks >= 2 && localY >= seaLevel + 10)
        {
            return PoiLootKind.Ruin;
        }

        if (supportBlocks >= 6 && localY >= seaLevel + 4)
        {
            return PoiLootKind.Watchtower;
        }

        if (torchBlocks >= 3 && cobblestoneBlocks >= 2 && localY <= seaLevel - 1)
        {
            return PoiLootKind.CaveCache;
        }

        if ((HasBlock(chunk, localX, localY + 1, localZ, BlockId.Gravel) || (gravelBlocks >= 2 && localY <= seaLevel + 3)) &&
            supportBlocks <= 1 &&
            cobblestoneBlocks <= 1 &&
            torchBlocks <= 1)
        {
            return PoiLootKind.BuriedCache;
        }

        return PoiLootKind.Generic;
    }

    private static bool HasBlock(Chunk chunk, int x, int y, int z, BlockId block)
    {
        return chunk.GetBlock(x, y, z) == block;
    }
}
