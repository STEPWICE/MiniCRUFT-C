using System.Globalization;
using System.Text;

namespace MiniCRUFT.UI;

public static class PauseHudText
{
    public static string BuildPauseMenuText(int selectedIndex)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Paused");
        sb.AppendLine("Up/Down: move");
        sb.AppendLine("Enter/Space: select");
        sb.AppendLine("Esc/Tab: resume");
        sb.AppendLine("F11: fullscreen");
        sb.AppendLine();

        AppendItem(sb, selectedIndex == 0, "Resume");
        AppendItem(sb, selectedIndex == 1, "Settings");
        AppendItem(sb, selectedIndex == 2, "Quit to desktop");
        return sb.ToString().TrimEnd();
    }

    public static string BuildSettingsMenuText(int selectedIndex, bool fullscreen, float fieldOfView, float mouseSensitivity, bool debugHud)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Settings");
        sb.AppendLine("Up/Down: move");
        sb.AppendLine("Enter/Space: toggle or select");
        sb.AppendLine("Left/Right: adjust values");
        sb.AppendLine("Esc: back  Tab: resume");
        sb.AppendLine("F11: fullscreen");
        sb.AppendLine("Changes save automatically");
        sb.AppendLine();

        AppendItem(sb, selectedIndex == 0, $"Fullscreen: {FormatToggle(fullscreen)}");
        AppendItem(sb, selectedIndex == 1, $"Field of view: {FormatFov(fieldOfView)}");
        AppendItem(sb, selectedIndex == 2, $"Mouse sensitivity: {mouseSensitivity.ToString("0.00", CultureInfo.InvariantCulture)}");
        AppendItem(sb, selectedIndex == 3, $"Debug HUD: {FormatToggle(debugHud)}");
        AppendItem(sb, selectedIndex == 4, "Back");
        return sb.ToString().TrimEnd();
    }

    private static void AppendItem(StringBuilder sb, bool selected, string text)
    {
        sb.AppendLine(selected ? $"> {text}" : $"  {text}");
    }

    private static string FormatToggle(bool value)
    {
        return value ? "On" : "Off";
    }

    private static string FormatFov(float value)
    {
        return value.ToString("0", CultureInfo.InvariantCulture) + " deg";
    }
}
