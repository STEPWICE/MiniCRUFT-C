using MiniCRUFT.UI;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class PauseHudTextTests
{
    [Fact]
    public void BuildPauseMenuText_ShowsSelectionAndResumeHint()
    {
        string text = PauseHudText.BuildPauseMenuText(selectedIndex: 1);

        Assert.Contains("Paused", text);
        Assert.Contains("Up/Down: move", text);
        Assert.Contains("Enter/Space: select", text);
        Assert.Contains("Esc/Tab: resume", text);
        Assert.Contains("F11: fullscreen", text);
        Assert.Contains("> Settings", text);
        Assert.Contains("  Resume", text);
        Assert.Contains("  Quit to desktop", text);
    }

    [Fact]
    public void BuildSettingsMenuText_ShowsCurrentValues()
    {
        string text = PauseHudText.BuildSettingsMenuText(
            selectedIndex: 2,
            fullscreen: true,
            fieldOfView: 90f,
            mouseSensitivity: 0.18f,
            debugHud: false);

        Assert.Contains("Settings", text);
        Assert.Contains("Up/Down: move", text);
        Assert.Contains("Left/Right: adjust values", text);
        Assert.Contains("Changes save automatically", text);
        Assert.Contains("Fullscreen: On", text);
        Assert.Contains("Field of view: 90 deg", text);
        Assert.Contains("Mouse sensitivity: 0.18", text);
        Assert.Contains("Debug HUD: Off", text);
        Assert.Contains("> Mouse sensitivity: 0.18", text);
        Assert.Contains("Back", text);
    }
}
