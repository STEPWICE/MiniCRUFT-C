using System;
using System.Numerics;

namespace MiniCRUFT.Core;

public static class ColorSpace
{
    public static float SrgbToLinear(float c)
    {
        if (c <= 0.04045f)
        {
            return c / 12.92f;
        }

        return MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    }

    public static float LinearToSrgb(float c)
    {
        if (c <= 0.0031308f)
        {
            return 12.92f * c;
        }

        return 1.055f * MathF.Pow(c, 1f / 2.4f) - 0.055f;
    }

    public static Vector3 ToLinear(Vector3 srgb)
    {
        return new Vector3(
            SrgbToLinear(srgb.X),
            SrgbToLinear(srgb.Y),
            SrgbToLinear(srgb.Z));
    }

    public static Vector4 ToLinear(Vector4 srgb)
    {
        var rgb = ToLinear(new Vector3(srgb.X, srgb.Y, srgb.Z));
        return new Vector4(rgb, srgb.W);
    }

    public static Color3 ToLinear(Color3 srgb)
    {
        return new Color3(
            SrgbToLinear(srgb.R),
            SrgbToLinear(srgb.G),
            SrgbToLinear(srgb.B));
    }

    public static Vector3 ToSrgb(Vector3 linear)
    {
        return new Vector3(
            LinearToSrgb(linear.X),
            LinearToSrgb(linear.Y),
            LinearToSrgb(linear.Z));
    }
}
