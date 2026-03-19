using MiniCRUFT.Core;
using MiniCRUFT.Game;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class BlockBreakSystemTests
{
    [Fact]
    public void TryStart_TracksProgressAndCompletesAfterDuration()
    {
        var system = new BlockBreakSystem();
        var target = new BlockCoord(4, 5, 6);

        Assert.True(system.TryStart(target, BlockId.Stone, BlockId.WoodenPickaxe, BlockId.Cobblestone, 1, true, 2f));
        Assert.True(system.IsActive);
        Assert.Equal(target, system.Target);
        Assert.False(system.Update(0.5f));
        Assert.InRange(system.Progress, 0.24f, 0.26f);

        Assert.True(system.Update(1.6f));
        Assert.False(system.IsActive);
        Assert.Equal(0f, system.Progress);

        system.Cancel();
        Assert.Equal(new BlockCoord(), system.Target);
        Assert.Equal(BlockId.Air, system.Block);
        Assert.Equal(BlockId.Air, system.Drop);
    }
}
