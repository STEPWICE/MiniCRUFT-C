using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class PoiLootClassifierTests
{
    [Fact]
    public void Classify_ReturnsCamp_WhenCampMarkersArePresent()
    {
        var chunk = new Chunk(0, 0);
        int x = 8;
        int y = 64;
        int z = 8;

        chunk.SetBlock(x + 1, y, z, BlockId.Furnace);
        chunk.SetBlock(x, y, z + 1, BlockId.CraftingTable);

        PoiLootKind kind = PoiLootClassifier.Classify(chunk, x, y, z, 62);

        Assert.Equal(PoiLootKind.Camp, kind);
    }

    [Fact]
    public void Classify_ReturnsMineShaft_WhenSupportsAndTorchesArePresent()
    {
        var chunk = new Chunk(0, 0);
        int x = 8;
        int y = 20;
        int z = 8;

        FillMineShaftSignature(chunk, x, y, z);

        PoiLootKind kind = PoiLootClassifier.Classify(chunk, x, y, z, 62);

        Assert.Equal(PoiLootKind.MineShaft, kind);
    }

    [Fact]
    public void Classify_ReturnsRuin_WhenCobblestoneAndTorchesArePresent()
    {
        var chunk = new Chunk(0, 0);
        int x = 8;
        int y = 90;
        int z = 8;

        FillRuinSignature(chunk, x, y, z);

        PoiLootKind kind = PoiLootClassifier.Classify(chunk, x, y, z, 62);

        Assert.Equal(PoiLootKind.Ruin, kind);
    }

    [Fact]
    public void Classify_ReturnsBuriedCache_WhenGravelIsDirectlyAboveChest()
    {
        var chunk = new Chunk(0, 0);
        int x = 8;
        int y = 64;
        int z = 8;

        chunk.SetBlock(x, y + 1, z, BlockId.Gravel);

        PoiLootKind kind = PoiLootClassifier.Classify(chunk, x, y, z, 62);

        Assert.Equal(PoiLootKind.BuriedCache, kind);
    }

    private static void FillMineShaftSignature(Chunk chunk, int x, int y, int z)
    {
        chunk.SetBlock(x + 1, y, z, BlockId.Wood);
        chunk.SetBlock(x - 1, y, z, BlockId.Planks);
        chunk.SetBlock(x, y, z + 1, BlockId.BirchWood);
        chunk.SetBlock(x, y, z - 1, BlockId.SpruceWood);
        chunk.SetBlock(x + 1, y + 1, z, BlockId.Wood);
        chunk.SetBlock(x - 1, y + 1, z, BlockId.Planks);
        chunk.SetBlock(x, y + 1, z + 1, BlockId.Wood);
        chunk.SetBlock(x + 1, y, z + 1, BlockId.Torch);
        chunk.SetBlock(x - 1, y, z - 1, BlockId.TorchWallNorth);
    }

    private static void FillRuinSignature(Chunk chunk, int x, int y, int z)
    {
        chunk.SetBlock(x + 1, y, z, BlockId.Cobblestone);
        chunk.SetBlock(x - 1, y, z, BlockId.Cobblestone);
        chunk.SetBlock(x, y, z + 1, BlockId.Cobblestone);
        chunk.SetBlock(x, y, z - 1, BlockId.Cobblestone);
        chunk.SetBlock(x + 1, y + 1, z, BlockId.Cobblestone);
        chunk.SetBlock(x - 1, y + 1, z, BlockId.Cobblestone);
        chunk.SetBlock(x + 1, y, z + 1, BlockId.Torch);
        chunk.SetBlock(x - 1, y, z - 1, BlockId.TorchWallSouth);
    }
}
