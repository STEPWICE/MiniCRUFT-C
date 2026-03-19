using System;
using System.IO;

namespace MiniCRUFT.IO;

public static class WorldReset
{
    public static bool TryReset(string worldPath, int seed, out string error)
    {
        try
        {
            if (Directory.Exists(worldPath))
            {
                Directory.Delete(worldPath, true);
            }

            Directory.CreateDirectory(worldPath);
            WorldSave.SaveSeed(worldPath, seed);
            error = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static void ResetOrThrow(string worldPath, int seed)
    {
        if (TryReset(worldPath, seed, out var error))
        {
            return;
        }

        throw new IOException($"Failed to reset world at '{worldPath}': {error}");
    }
}
