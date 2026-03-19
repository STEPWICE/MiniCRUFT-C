namespace MiniCRUFT.Game;

public readonly struct CharacterLiquidState
{
    public bool InWater { get; }
    public bool InLava { get; }
    public float WaterCoverage { get; }
    public float LavaCoverage { get; }

    public bool InLiquid => InWater || InLava;
    public float Coverage => MathF.Max(WaterCoverage, LavaCoverage);

    public CharacterLiquidState(bool inWater, bool inLava, float waterCoverage, float lavaCoverage)
    {
        InWater = inWater;
        InLava = inLava;
        WaterCoverage = Math.Clamp(waterCoverage, 0f, 1f);
        LavaCoverage = Math.Clamp(lavaCoverage, 0f, 1f);
    }
}
