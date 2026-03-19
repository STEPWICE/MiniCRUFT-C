using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.Renderer;

internal static class MobModelBuilder
{
    private const float FaceTopShade = 1.0f;
    private const float FaceSideShade = 0.84f;
    private const float FaceFrontShade = 0.90f;
    private const float FaceBottomShade = 0.58f;
    private const float PivotEpsilon = 0.0001f;

    public static void AppendMob(
        List<MobVertex> vertices,
        List<ushort> indices,
        MobRenderInstance mob,
        SpriteRegion region,
        MobModelDefinition definition,
        bool overlayPass = false)
    {
        float scale = mob.Height / definition.BaseHeightPixels;
        if (overlayPass && definition.OverlayInflationPixels > 0f)
        {
            scale *= 1f + definition.OverlayInflationPixels / definition.BaseHeightPixels;
        }

        float horizontalSpeed = new Vector2(mob.Velocity.X, mob.Velocity.Z).Length();
        float motion = Math.Clamp(horizontalSpeed / Math.Max(definition.MotionReferenceSpeed, 0.0001f), 0f, 1f);
        float walkCycle = mob.Age * definition.BobFrequency + horizontalSpeed * 1.75f;
        float bobFactor = mob.OnGround ? (0.25f + motion * 0.75f) : 0.12f;
        float bobOffsetWorld = MathF.Sin(mob.Age * definition.BobFrequency) * definition.BobAmplitudePixels * scale * bobFactor;
        float yaw = mob.Yaw + definition.YawOffsetRadians;
        Vector3 root = mob.Position + new Vector3(0f, 0.01f + bobOffsetWorld, 0f);

        float healthShade = 0.78f + mob.HealthRatio * 0.22f;
        Vector4 tint = new(mob.Tint.X * healthShade, mob.Tint.Y * healthShade, mob.Tint.Z * healthShade, mob.Tint.W);
        Vector2 effects = new(mob.HurtFlash, Math.Max(mob.SpecialProgress, mob.StaggerProgress));
        if (mob.Elite)
        {
            tint = new Vector4(
                Math.Min(1.35f, tint.X * 1.18f + 0.08f),
                Math.Min(1.25f, tint.Y * 1.12f + 0.05f),
                Math.Min(1.00f, tint.Z * 0.80f + 0.12f),
                tint.W);
            effects = new Vector2(effects.X, Math.Max(effects.Y, 0.12f));

            switch (mob.EliteVariant)
            {
                case EliteMobVariant.Brute:
                    tint = new Vector4(
                        Math.Min(1.4f, tint.X * 1.08f + 0.04f),
                        Math.Min(1.2f, tint.Y * 1.00f + 0.03f),
                        Math.Min(1.0f, tint.Z * 0.78f + 0.06f),
                        tint.W);
                    effects = new Vector2(effects.X, Math.Max(effects.Y, 0.15f));
                    break;
                case EliteMobVariant.Hunter:
                    tint = new Vector4(
                        Math.Min(1.15f, tint.X * 0.88f),
                        Math.Min(1.35f, tint.Y * 1.12f + 0.08f),
                        Math.Min(1.4f, tint.Z * 1.15f + 0.1f),
                        tint.W);
                    effects = new Vector2(effects.X, Math.Max(effects.Y, 0.18f));
                    break;
                case EliteMobVariant.Warden:
                    tint = new Vector4(
                        Math.Min(1.2f, tint.X * 0.92f + 0.02f),
                        Math.Min(1.35f, tint.Y * 1.16f + 0.08f),
                        Math.Min(1.35f, tint.Z * 1.05f + 0.08f),
                        tint.W);
                    effects = new Vector2(effects.X, Math.Max(effects.Y, 0.22f));
                    break;
            }
        }

        for (int i = 0; i < definition.Boxes.Count; i++)
        {
            AddBox(
                vertices,
                indices,
                root,
                scale,
                yaw,
                definition.Boxes[i],
                region,
                tint,
                effects,
                overlayPass ? definition.OverlayInflationPixels : 0f,
                walkCycle,
                mob.Age,
                motion,
                definition.LegSwingRadians,
                definition.WingFlapFrequency,
                definition.WingFlapRadians,
                definition.SpecialPulseScale,
                mob.SpecialProgress);
        }
    }

    private static void AddBox(
        List<MobVertex> vertices,
        List<ushort> indices,
        Vector3 root,
        float scale,
        float yaw,
        MobBox box,
        SpriteRegion region,
        Vector4 tint,
        Vector2 effects,
        float inflatePixels,
        float walkCycle,
        float age,
        float motion,
        float legSwingRadians,
        float wingFlapFrequency,
        float wingFlapRadians,
        float specialPulseScale,
        float specialProgress)
    {
        Vector3 min = box.MinPixels;
        Vector3 size = box.SizePixels;
        if (inflatePixels > 0f)
        {
            Vector3 inflate = new(inflatePixels, inflatePixels, inflatePixels);
            min -= inflate;
            size += inflate * 2f;
        }

        float pulseScale = specialPulseScale > 0f ? 1f + specialProgress * specialPulseScale : 1f;
        float partScale = scale * pulseScale;

        Vector3 max = min + size;
        Vector3 center = (min + max) * 0.5f;
        Vector3 topCenter = new(center.X, max.Y, center.Z);
        Vector3 bottomCenter = new(center.X, min.Y, center.Z);

        float pitch = 0f;
        float roll = 0f;
        switch (box.Kind)
        {
            case MobPartKind.LegFrontLeft:
            case MobPartKind.LegBackRight:
                pitch = MathF.Sin(walkCycle) * legSwingRadians * motion;
                break;
            case MobPartKind.LegFrontRight:
            case MobPartKind.LegBackLeft:
                pitch = MathF.Sin(walkCycle + MathF.PI) * legSwingRadians * motion;
                break;
            case MobPartKind.ArmLeft:
                pitch = MathF.Sin(walkCycle + MathF.PI) * legSwingRadians * motion * 0.82f;
                break;
            case MobPartKind.ArmRight:
                pitch = MathF.Sin(walkCycle) * legSwingRadians * motion * 0.82f;
                break;
            case MobPartKind.WingLeft:
                roll = wingFlapFrequency <= 0f ? 0f : MathF.Sin(age * wingFlapFrequency) * wingFlapRadians;
                break;
            case MobPartKind.WingRight:
                roll = wingFlapFrequency <= 0f ? 0f : -MathF.Sin(age * wingFlapFrequency) * wingFlapRadians;
                break;
            case MobPartKind.Head:
                pitch = MathF.Sin(walkCycle * 0.22f) * 0.03f * motion;
                break;
        }

        Vector3[] corners =
        [
            min,
            new Vector3(max.X, min.Y, min.Z),
            new Vector3(max.X, max.Y, min.Z),
            new Vector3(min.X, max.Y, min.Z),
            new Vector3(min.X, min.Y, max.Z),
            new Vector3(max.X, min.Y, max.Z),
            max,
            new Vector3(min.X, max.Y, max.Z)
        ];

        Vector3 pivot = box.Kind switch
        {
            MobPartKind.LegFrontLeft or MobPartKind.LegFrontRight or MobPartKind.LegBackLeft or MobPartKind.LegBackRight or MobPartKind.ArmLeft or MobPartKind.ArmRight or MobPartKind.WingLeft or MobPartKind.WingRight => topCenter,
            MobPartKind.Head => bottomCenter,
            MobPartKind.Body => center,
            _ => Vector3.Zero
        };

        Vector2 regionExtent = region.Max - region.Min;
        float textureWidth = Math.Max(1f, region.Size.X);
        float textureHeight = Math.Max(1f, region.Size.Y);
        Vector2 ToUv(float u, float v) => region.Min + new Vector2(u / textureWidth, v / textureHeight) * regionExtent;

        float u = box.TextureOriginPixels.X;
        float v = box.TextureOriginPixels.Y;
        float w = size.X;
        float h = size.Y;
        float d = size.Z;

        Vector2 uvLeftMin = ToUv(u, v + d);
        Vector2 uvLeftMax = ToUv(u + d, v + d + h);
        Vector2 uvFrontMin = ToUv(u + d, v + d);
        Vector2 uvFrontMax = ToUv(u + d + w, v + d + h);
        Vector2 uvRightMin = ToUv(u + d + w, v + d);
        Vector2 uvRightMax = ToUv(u + d + w + d, v + d + h);
        Vector2 uvBackMin = ToUv(u + d + w + d, v + d);
        Vector2 uvBackMax = ToUv(u + d + w + d + w, v + d + h);
        Vector2 uvTopMin = ToUv(u + d, v);
        Vector2 uvTopMax = ToUv(u + d + w, v + d);
        Vector2 uvBottomMin = ToUv(u + d + w, v + d);
        Vector2 uvBottomMax = ToUv(u + d + w + w, v + d + d);

        AddFace(vertices, indices, corners[4], corners[5], corners[6], corners[7], uvFrontMin, uvFrontMax, tint, effects, box.Shade * FaceFrontShade, root, yaw, partScale, pitch, roll, pivot);
        AddFace(vertices, indices, corners[1], corners[0], corners[3], corners[2], uvBackMin, uvBackMax, tint, effects, box.Shade * FaceFrontShade, root, yaw, partScale, pitch, roll, pivot);
        AddFace(vertices, indices, corners[0], corners[4], corners[7], corners[3], uvLeftMin, uvLeftMax, tint, effects, box.Shade * FaceSideShade, root, yaw, partScale, pitch, roll, pivot);
        AddFace(vertices, indices, corners[5], corners[1], corners[2], corners[6], uvRightMin, uvRightMax, tint, effects, box.Shade * FaceSideShade, root, yaw, partScale, pitch, roll, pivot);
        AddFace(vertices, indices, corners[3], corners[7], corners[6], corners[2], uvTopMin, uvTopMax, tint, effects, box.Shade * FaceTopShade, root, yaw, partScale, pitch, roll, pivot);
        AddFace(vertices, indices, corners[0], corners[1], corners[5], corners[4], uvBottomMin, uvBottomMax, tint, effects, box.Shade * FaceBottomShade, root, yaw, partScale, pitch, roll, pivot);
    }

    private static void AddFace(
        List<MobVertex> vertices,
        List<ushort> indices,
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        Vector3 v3,
        Vector2 uvMin,
        Vector2 uvMax,
        Vector4 tint,
        Vector2 effects,
        float shade,
        Vector3 root,
        float yaw,
        float scale,
        float pitch,
        float roll,
        Vector3 pivot)
    {
        if (vertices.Count > ushort.MaxValue - 4)
        {
            return;
        }

        Vector3 p0 = TransformPoint(v0, pivot, pitch, roll, yaw, root, scale);
        Vector3 p1 = TransformPoint(v1, pivot, pitch, roll, yaw, root, scale);
        Vector3 p2 = TransformPoint(v2, pivot, pitch, roll, yaw, root, scale);
        Vector3 p3 = TransformPoint(v3, pivot, pitch, roll, yaw, root, scale);

        Vector4 faceTint = new(tint.X * shade, tint.Y * shade, tint.Z * shade, tint.W);
        ushort start = (ushort)vertices.Count;
        vertices.Add(new MobVertex(p0, new Vector2(uvMin.X, uvMax.Y), faceTint, effects));
        vertices.Add(new MobVertex(p1, new Vector2(uvMax.X, uvMax.Y), faceTint, effects));
        vertices.Add(new MobVertex(p2, new Vector2(uvMax.X, uvMin.Y), faceTint, effects));
        vertices.Add(new MobVertex(p3, new Vector2(uvMin.X, uvMin.Y), faceTint, effects));

        indices.Add(start);
        indices.Add((ushort)(start + 1));
        indices.Add((ushort)(start + 2));
        indices.Add(start);
        indices.Add((ushort)(start + 2));
        indices.Add((ushort)(start + 3));
    }

    private static Vector3 TransformPoint(Vector3 point, Vector3 pivot, float pitch, float roll, float yaw, Vector3 root, float scale)
    {
        Vector3 result = point;
        if (MathF.Abs(pitch) > PivotEpsilon)
        {
            result = RotateAroundX(result, pivot, pitch);
        }

        if (MathF.Abs(roll) > PivotEpsilon)
        {
            result = RotateAroundZ(result, pivot, roll);
        }

        if (MathF.Abs(yaw) > PivotEpsilon)
        {
            result = RotateAroundY(result, Vector3.Zero, yaw);
        }

        return root + result * scale;
    }

    private static Vector3 RotateAroundX(Vector3 point, Vector3 pivot, float angle)
    {
        float sin = MathF.Sin(angle);
        float cos = MathF.Cos(angle);
        float y = point.Y - pivot.Y;
        float z = point.Z - pivot.Z;
        return new Vector3(point.X, pivot.Y + y * cos - z * sin, pivot.Z + y * sin + z * cos);
    }

    private static Vector3 RotateAroundY(Vector3 point, Vector3 pivot, float angle)
    {
        float sin = MathF.Sin(angle);
        float cos = MathF.Cos(angle);
        float x = point.X - pivot.X;
        float z = point.Z - pivot.Z;
        return new Vector3(pivot.X + x * cos - z * sin, point.Y, pivot.Z + x * sin + z * cos);
    }

    private static Vector3 RotateAroundZ(Vector3 point, Vector3 pivot, float angle)
    {
        float sin = MathF.Sin(angle);
        float cos = MathF.Cos(angle);
        float x = point.X - pivot.X;
        float y = point.Y - pivot.Y;
        return new Vector3(pivot.X + x * cos - y * sin, pivot.Y + x * sin + y * cos, point.Z);
    }
}
