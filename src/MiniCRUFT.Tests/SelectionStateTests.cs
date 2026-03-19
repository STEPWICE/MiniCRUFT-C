using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.Renderer;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class SelectionStateTests
{
    [Fact]
    public void ForBlock_CreatesBlockSelection()
    {
        var selection = SelectionState.ForBlock(new BlockCoord(3, 4, 5), 2.5f);

        Assert.True(selection.HasSelection);
        Assert.True(selection.IsBlock);
        Assert.False(selection.IsMob);
        Assert.Equal(new BlockCoord(3, 4, 5), selection.Block);
        Assert.Equal(2.5f, selection.Distance);
        Assert.Equal(0f, selection.Progress);
    }

    [Fact]
    public void ForBlock_WithProgress_CapturesProgress()
    {
        var selection = SelectionState.ForBlock(new BlockCoord(1, 2, 3), 1.5f, 0.75f);

        Assert.Equal(0.75f, selection.Progress);
    }

    [Fact]
    public void ForMob_CreatesMobSelection()
    {
        var mob = new MobRenderInstance(
            MobType.Zombie,
            new Vector3(10f, 20f, 30f),
            new Vector3(0f, 0f, 0f),
            0.6f,
            1.8f,
            20,
            20,
            0f,
            onGround: true,
            hurtFlash: 0.5f,
            specialProgress: 0f,
            age: 1.5f,
            Vector4.One);

        var selection = SelectionState.ForMob(mob, 4.5f);

        Assert.True(selection.HasSelection);
        Assert.False(selection.IsBlock);
        Assert.True(selection.IsMob);
        Assert.Equal(mob, selection.Mob);
        Assert.Equal(4.5f, selection.Distance);
    }
}
