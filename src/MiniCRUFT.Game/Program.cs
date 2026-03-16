using System;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

internal static class Program
{
    private static void Main()
    {
        Log.Initialize(System.IO.Path.Combine("logs", "engine.log"));
        try
        {
            Log.Info($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Log.Info($"OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            Log.Info($"BaseDir: {AppContext.BaseDirectory}");
            const string configPath = "config.json";
            var config = GameConfig.LoadOrCreate(configPath);
            if (config.ResetWorldOnLaunch)
            {
                ResetWorld(config);
                config.ResetWorldOnLaunch = false;
                GameConfig.Save(configPath, config);
            }
            using var app = new GameApp(config);
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Error($"Fatal: {ex}");
            throw;
        }
    }

    private static void ResetWorld(GameConfig config)
    {
        try
        {
            if (System.IO.Directory.Exists(config.WorldPath))
            {
                System.IO.Directory.Delete(config.WorldPath, true);
            }
            System.IO.Directory.CreateDirectory(config.WorldPath);
            Log.Warn("World reset: cleared existing world data.");
        }
        catch (Exception ex)
        {
            Log.Warn($"World reset failed: {ex.Message}");
        }
    }
}
