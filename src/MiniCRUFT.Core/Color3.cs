using System.Numerics;

namespace MiniCRUFT.Core;

public struct Color3
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }

    public Color3(float r, float g, float b)
    {
        R = r;
        G = g;
        B = b;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(R, G, B);
    }

    public Vector4 ToVector4(float a = 1f)
    {
        return new Vector4(R, G, B, a);
    }

    public static Color3 FromBytes(byte r, byte g, byte b)
    {
        return new Color3(r / 255f, g / 255f, b / 255f);
    }
}
