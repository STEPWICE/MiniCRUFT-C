using MiniCRUFT.Renderer;
using Veldrid;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class RenderDeviceTests
{
    [Fact]
    public void GetInitialWindowState_ReturnsNormal_ForWindowedStartup()
    {
        Assert.Equal(WindowState.Normal, RenderDevice.GetInitialWindowState(false));
    }

    [Fact]
    public void GetInitialWindowState_ReturnsBorderlessFullscreen_ForFullscreenStartup()
    {
        Assert.Equal(WindowState.BorderlessFullScreen, RenderDevice.GetInitialWindowState(true));
    }

    [Fact]
    public void GetCenteredWindowPosition_CentersWithinDisplayBounds()
    {
        var position = RenderDevice.GetCenteredWindowPosition(1920, 0, 1920, 1080, 1280, 720);

        Assert.Equal(2240, position.X);
        Assert.Equal(180, position.Y);
    }

    [Fact]
    public void GetCenteredWindowPosition_ClampsToDisplayOrigin_WhenWindowIsLargerThanDisplay()
    {
        var position = RenderDevice.GetCenteredWindowPosition(-1280, 0, 1280, 720, 1600, 900);

        Assert.Equal(-1280, position.X);
        Assert.Equal(0, position.Y);
    }
}
