using System;

namespace MiniCRUFT.Core;

public static class MathUtil
{
    public static float Clamp(float value, float min, float max) => MathF.Max(min, MathF.Min(max, value));
    public static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));

    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    public static int FloorToInt(float value) => (int)MathF.Floor(value);
    public static int CeilToInt(float value) => (int)MathF.Ceiling(value);
}
