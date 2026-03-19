using System;
using MiniCRUFT.Core;
using MiniCRUFT.IO;

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
            bool centerWindowOnStart = !System.IO.File.Exists(configPath);
            var config = GameConfig.LoadOrCreate(configPath);
            if (config.ResetWorldOnLaunch)
            {
                WorldReset.ResetOrThrow(config.WorldPath, config.Seed);
                config.ResetWorldOnLaunch = false;
                try
                {
                    GameConfig.Save(configPath, config);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Failed to persist reset flag after world reset: {ex.Message}");
                }
            }
            using var app = new GameApp(config, configPath, centerWindowOnStart);
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Error($"Fatal: {ex}");
            throw;
        }
    }
}
