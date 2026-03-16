using System.Numerics;

namespace MiniCRUFT.Renderer;

public readonly struct Aabb
{
    public Vector3 Min { get; }
    public Vector3 Max { get; }

    public Aabb(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }
}

public readonly struct Frustum
{
    private readonly Plane _left;
    private readonly Plane _right;
    private readonly Plane _top;
    private readonly Plane _bottom;
    private readonly Plane _near;
    private readonly Plane _far;

    private Frustum(Plane left, Plane right, Plane top, Plane bottom, Plane near, Plane far)
    {
        _left = left;
        _right = right;
        _top = top;
        _bottom = bottom;
        _near = near;
        _far = far;
    }

    public static Frustum FromMatrix(Matrix4x4 matrix)
    {
        var left = Plane.Normalize(new Plane(
            matrix.M14 + matrix.M11,
            matrix.M24 + matrix.M21,
            matrix.M34 + matrix.M31,
            matrix.M44 + matrix.M41));

        var right = Plane.Normalize(new Plane(
            matrix.M14 - matrix.M11,
            matrix.M24 - matrix.M21,
            matrix.M34 - matrix.M31,
            matrix.M44 - matrix.M41));

        var bottom = Plane.Normalize(new Plane(
            matrix.M14 + matrix.M12,
            matrix.M24 + matrix.M22,
            matrix.M34 + matrix.M32,
            matrix.M44 + matrix.M42));

        var top = Plane.Normalize(new Plane(
            matrix.M14 - matrix.M12,
            matrix.M24 - matrix.M22,
            matrix.M34 - matrix.M32,
            matrix.M44 - matrix.M42));

        var near = Plane.Normalize(new Plane(
            matrix.M13,
            matrix.M23,
            matrix.M33,
            matrix.M43));

        var far = Plane.Normalize(new Plane(
            matrix.M14 - matrix.M13,
            matrix.M24 - matrix.M23,
            matrix.M34 - matrix.M33,
            matrix.M44 - matrix.M43));

        return new Frustum(left, right, top, bottom, near, far);
    }

    public bool Intersects(Aabb box)
    {
        return IntersectsPlane(_left, box) &&
               IntersectsPlane(_right, box) &&
               IntersectsPlane(_top, box) &&
               IntersectsPlane(_bottom, box) &&
               IntersectsPlane(_near, box) &&
               IntersectsPlane(_far, box);
    }

    private static bool IntersectsPlane(Plane plane, Aabb box)
    {
        Vector3 positive = new(
            plane.Normal.X >= 0 ? box.Max.X : box.Min.X,
            plane.Normal.Y >= 0 ? box.Max.Y : box.Min.Y,
            plane.Normal.Z >= 0 ? box.Max.Z : box.Min.Z);

        return Plane.DotCoordinate(plane, positive) >= 0;
    }
}
