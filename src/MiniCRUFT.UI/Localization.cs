using System;
using System.Collections.Generic;
using System.Text.Json;
using MiniCRUFT.Core;

namespace MiniCRUFT.UI;

public sealed class Localization
{
    private Dictionary<string, string> _strings = new();

    public Localization(AssetStore assets, string relativePath)
    {
        try
        {
            var text = assets.ReadAllText(relativePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(text);
            if (data != null)
            {
                _strings = data;
                return;
            }

            Log.Warn($"Localization file {relativePath} is empty or unsupported; falling back to key names.");
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to load localization file {relativePath}: {ex.Message}");
            _strings = new Dictionary<string, string>();
        }
    }

    public string Get(string key)
    {
        return _strings.TryGetValue(key, out var value) ? value : key;
    }
}
