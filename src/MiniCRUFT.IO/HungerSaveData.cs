namespace MiniCRUFT.IO;

public readonly struct HungerSaveData
{
    public float Hunger { get; }
    public float StarvationTimer { get; }

    public HungerSaveData(float hunger, float starvationTimer)
    {
        Hunger = hunger;
        StarvationTimer = starvationTimer;
    }
}
