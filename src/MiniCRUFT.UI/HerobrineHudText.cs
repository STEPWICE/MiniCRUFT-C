using System.Globalization;
using System.Text;
using MiniCRUFT.Core;

namespace MiniCRUFT.UI;

public static class HerobrineHudText
{
    public static string BuildStatusText(bool enabled, string mode, bool manifested, float pressure, bool controlsAvailable)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Herobrine");
        sb.AppendLine($"Mode: {NormalizeMode(mode)}");

        if (!enabled)
        {
            sb.AppendLine("State: Disabled");
            if (controlsAvailable)
            {
                sb.AppendLine("F8: Enable");
                sb.AppendLine("F9: Change mode");
            }

            return sb.ToString().TrimEnd();
        }

        sb.AppendLine($"State: {(manifested ? "Manifested" : "Watching")}");
        sb.AppendLine($"Pressure: {FormatPressure(pressure)}");
        if (controlsAvailable)
        {
            sb.AppendLine("F8: Disable");
            sb.AppendLine("F9: Change mode");
        }

        return sb.ToString().TrimEnd();
    }

    public static string BuildPauseMenuText(bool enabled, string mode, string modeDescription, bool manifested, float pressure, bool controlsAvailable)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Paused");
        sb.AppendLine($"Herobrine: {(enabled ? "On" : "Off")}");
        sb.AppendLine($"Mode: {NormalizeMode(mode)}");
        if (!string.IsNullOrWhiteSpace(modeDescription))
        {
            sb.AppendLine(modeDescription.Trim());
        }

        if (enabled)
        {
            sb.AppendLine($"State: {(manifested ? "Manifested" : "Watching")}");
            sb.AppendLine($"Pressure: {FormatPressure(pressure)}");
            sb.AppendLine("Pressure: more manifests, cues, world edits");
            sb.AppendLine("Higher: night, caves, encounters");
            sb.AppendLine("Lower: daylight, open air, disable");
        }

        if (controlsAvailable)
        {
            sb.AppendLine(enabled ? "F8: Disable" : "F8: Enable");
            sb.AppendLine("F9: Change mode");
        }
        else
        {
            sb.AppendLine("Herobrine controls disabled in strict beta");
        }

        sb.Append("Tab: resume");
        return sb.ToString();
    }

    public static string BuildToastText(bool enabled)
    {
        return enabled ? "Herobrine enabled" : "Herobrine disabled";
    }

    public static string BuildModeToastText(string mode, string modeDescription)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Herobrine mode: {NormalizeMode(mode)}");
        if (!string.IsNullOrWhiteSpace(modeDescription))
        {
            sb.AppendLine(modeDescription.Trim());
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatPressure(float pressure)
    {
        float value = Math.Clamp(pressure, 0f, 1f) * 100f;
        return value.ToString("0", CultureInfo.InvariantCulture) + "%";
    }

    private static string NormalizeMode(string mode)
    {
        return HerobrineModeCatalog.Normalize(mode);
    }
}
