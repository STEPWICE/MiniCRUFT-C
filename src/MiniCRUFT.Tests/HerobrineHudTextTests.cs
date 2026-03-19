using MiniCRUFT.UI;
using Xunit;

namespace MiniCRUFT.Tests;

public sealed class HerobrineHudTextTests
{
    [Fact]
    public void BuildStatusText_WhenDisabled_ShowsEnableHint()
    {
        string text = HerobrineHudText.BuildStatusText(enabled: false, mode: "Classic", manifested: false, pressure: 0f, controlsAvailable: true);

        Assert.Contains("Herobrine", text);
        Assert.Contains("Mode: Classic", text);
        Assert.Contains("State: Disabled", text);
        Assert.Contains("F8: Enable", text);
        Assert.Contains("F9: Change mode", text);
    }

    [Fact]
    public void BuildStatusText_WhenActive_ShowsPressureAndDisableHint()
    {
        string text = HerobrineHudText.BuildStatusText(enabled: true, mode: "Nightmare", manifested: true, pressure: 0.42f, controlsAvailable: true);

        Assert.Contains("Mode: Nightmare", text);
        Assert.Contains("State: Manifested", text);
        Assert.Contains("Pressure: 42%", text);
        Assert.Contains("F8: Disable", text);
        Assert.Contains("F9: Change mode", text);
    }

    [Fact]
    public void BuildPauseMenuText_WhenToggleUnavailable_ShowsStrictBetaNotice()
    {
        string text = HerobrineHudText.BuildPauseMenuText(enabled: true, mode: "Stalker", modeDescription: "Appears behind you more often.", manifested: false, pressure: 0.1f, controlsAvailable: false);

        Assert.Contains("Paused", text);
        Assert.Contains("Herobrine: On", text);
        Assert.Contains("Mode: Stalker", text);
        Assert.Contains("Appears behind you more often.", text);
        Assert.Contains("Herobrine controls disabled in strict beta", text);
        Assert.Contains("Tab: resume", text);
    }

    [Fact]
    public void BuildToastText_UsesEnabledState()
    {
        Assert.Equal("Herobrine enabled", HerobrineHudText.BuildToastText(true));
        Assert.Equal("Herobrine disabled", HerobrineHudText.BuildToastText(false));
    }

    [Fact]
    public void BuildModeToastText_ShowsModeAndDescription()
    {
        string text = HerobrineHudText.BuildModeToastText("Haunt", "More whispers and world effects.");

        Assert.Contains("Herobrine mode: Haunt", text);
        Assert.Contains("More whispers and world effects.", text);
    }
}
