using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.Renderer;

public enum MobPartKind
{
    Static,
    Head,
    Body,
    LegFrontLeft,
    LegFrontRight,
    LegBackLeft,
    LegBackRight,
    ArmLeft,
    ArmRight,
    WingLeft,
    WingRight,
    Beak,
    HornLeft,
    HornRight
}

public readonly record struct MobBox(
    MobPartKind Kind,
    Vector3 MinPixels,
    Vector3 SizePixels,
    Vector2 TextureOriginPixels,
    float Shade = 1f);

public sealed class MobModelDefinition
{
    public MobType Type { get; }
    public string TextureName { get; }
    public string? OverlayTextureName { get; }
    public bool OverlayFullBright { get; }
    public float BaseHeightPixels { get; }
    public float YawOffsetRadians { get; }
    public float BobAmplitudePixels { get; }
    public float BobFrequency { get; }
    public float MotionReferenceSpeed { get; }
    public float LegSwingRadians { get; }
    public float WingFlapFrequency { get; }
    public float WingFlapRadians { get; }
    public float SpecialPulseScale { get; }
    public float OverlayInflationPixels { get; }
    public IReadOnlyList<MobBox> Boxes { get; }

    public MobModelDefinition(
        MobType type,
        string textureName,
        string? overlayTextureName,
        bool overlayFullBright,
        float baseHeightPixels,
        float yawOffsetRadians,
        float bobAmplitudePixels,
        float bobFrequency,
        float motionReferenceSpeed,
        float legSwingRadians,
        float wingFlapFrequency,
        float wingFlapRadians,
        float specialPulseScale,
        float overlayInflationPixels,
        IReadOnlyList<MobBox> boxes)
    {
        if (baseHeightPixels <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(baseHeightPixels));
        }

        Type = type;
        TextureName = textureName ?? throw new ArgumentNullException(nameof(textureName));
        OverlayTextureName = overlayTextureName;
        OverlayFullBright = overlayFullBright;
        BaseHeightPixels = baseHeightPixels;
        YawOffsetRadians = yawOffsetRadians;
        BobAmplitudePixels = bobAmplitudePixels;
        BobFrequency = bobFrequency;
        MotionReferenceSpeed = motionReferenceSpeed;
        LegSwingRadians = legSwingRadians;
        WingFlapFrequency = wingFlapFrequency;
        WingFlapRadians = wingFlapRadians;
        SpecialPulseScale = specialPulseScale;
        OverlayInflationPixels = overlayInflationPixels;
        Boxes = boxes ?? throw new ArgumentNullException(nameof(boxes));
    }
}
