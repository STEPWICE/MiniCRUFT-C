namespace MiniCRUFT.IO;

public readonly struct DayNightSaveData
{
    public float TimeOfDay { get; }
    public long DayCount { get; }

    public DayNightSaveData(float timeOfDay, long dayCount)
    {
        TimeOfDay = timeOfDay;
        DayCount = dayCount;
    }
}
