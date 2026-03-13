using System.Text.Json;
using MiniCRUFT.World;

namespace MiniCRUFT.Persistence;

public sealed class PersistenceHost
{
    public const int CurrentSaveFormatVersion = 1;
    private const int LegacyUnversionedSaveFormatVersion = 0;
    private const string DefaultSaveFilename = "world.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string GetDefaultSavePath()
    {
        return Path.GetFullPath(DefaultSaveFilename);
    }

    public bool SaveFileExists(string? filename = null)
    {
        var resolvedFilename = filename ?? GetDefaultSavePath();
        return File.Exists(resolvedFilename);
    }

    public void Initialize()
    {
    }

    public string SaveWorldToJson(WorldHost world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var snapshot = world.ExportSnapshot() with
        {
            FormatVersion = CurrentSaveFormatVersion
        };

        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public void SaveWorld(string filename, WorldHost world)
    {
        ArgumentNullException.ThrowIfNull(filename);
        File.WriteAllText(filename, SaveWorldToJson(world));
    }

    public WorldHost LoadWorldFromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        WorldSaveSnapshot snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<WorldSaveSnapshot>(json, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize world snapshot.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("World save JSON is malformed.", exception);
        }

        snapshot = NormalizeSnapshotVersion(snapshot);

        var world = new WorldHost();
        world.ImportSnapshot(snapshot);
        return world;
    }

    public WorldHost LoadWorld(string filename)
    {
        ArgumentNullException.ThrowIfNull(filename);
        return LoadWorldFromJson(File.ReadAllText(filename));
    }

    public void Shutdown()
    {
    }

    private static WorldSaveSnapshot NormalizeSnapshotVersion(WorldSaveSnapshot snapshot)
    {
        return snapshot.FormatVersion switch
        {
            CurrentSaveFormatVersion => snapshot,
            LegacyUnversionedSaveFormatVersion => snapshot with { FormatVersion = CurrentSaveFormatVersion },
            < 0 => throw new InvalidOperationException($"World save format version '{snapshot.FormatVersion}' is invalid."),
            _ => throw new InvalidOperationException($"World save format version '{snapshot.FormatVersion}' is unsupported.")
        };
    }
}
