namespace MiniCRUFT.Core;

public sealed class SaveConfig
{
    public bool EnableAutoSave { get; set; } = true;
    public float AutoSaveIntervalSeconds { get; set; } = 30f;
    public int SaveWorkers { get; set; } = 1;
    public int LoadWorkers { get; set; } = 1;
    public int UnloadExtraRadius { get; set; } = 2;
    public int MaxBlockChangesPerFrame { get; set; } = 4096;
}
