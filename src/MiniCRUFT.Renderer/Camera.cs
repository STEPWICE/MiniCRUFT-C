using System;
using System.Numerics;

namespace MiniCRUFT.Renderer;

public sealed class Camera
{
    public Vector3 Position { get; set; }
    public float Yaw { get; set; }
    public float Pitch { get; set; }
    public float Fov { get; set; } = 75f;
    public float AspectRatio { get; set; } = 16f / 9f;
    public float Near { get; set; } = 0.1f;
    public float Far { get; set; } = 2000f;

    public Matrix4x4 View { get; private set; }
    public Matrix4x4 Projection { get; private set; }

    public void UpdateMatrices()
    {
        var forward = GetForward();
        View = Matrix4x4.CreateLookAt(Position, Position + forward, Vector3.UnitY);
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI * Fov / 180f, AspectRatio, Near, Far);
    }

    public Vector3 GetForward()
    {
        float yawRad = MathF.PI * Yaw / 180f;
        float pitchRad = MathF.PI * Pitch / 180f;
        return Vector3.Normalize(new Vector3(
            MathF.Cos(pitchRad) * MathF.Cos(yawRad),
            MathF.Sin(pitchRad),
            MathF.Cos(pitchRad) * MathF.Sin(yawRad)
        ));
    }

    public Vector3 GetRight()
    {
        return Vector3.Normalize(Vector3.Cross(GetForward(), Vector3.UnitY));
    }
}
