using System;

namespace MiniCRUFT.Renderer;

public static class LightModel
{
    public static float ComputeAmbientOcclusion(bool side1, bool side2, bool corner, float min, float strength)
    {
        int occlusion = side1 && side2 ? 3 : (side1 ? 1 : 0) + (side2 ? 1 : 0) + (corner ? 1 : 0);
        float ao = 1f - occlusion * 0.2f;
        ao = Math.Clamp(ao, min, 1f);
        return 1f - (1f - ao) * strength;
    }

    public static float ComputeFogFactor(float dist, float start, float end, bool linear)
    {
        float denom = MathF.Max(0.001f, end - start);
        if (linear)
        {
            return Math.Clamp((end - dist) / denom, 0f, 1f);
        }

        float density = 1f / denom;
        return Math.Clamp(MathF.Exp(-dist * density), 0f, 1f);
    }
}
