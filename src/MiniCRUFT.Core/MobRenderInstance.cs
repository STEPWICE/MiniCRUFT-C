using System;
using System.Numerics;

namespace MiniCRUFT.Core;

public readonly struct MobRenderInstance
{
    public MobType Type { get; }
    public Vector3 Position { get; }
    public Vector3 Velocity { get; }
    public float Width { get; }
    public float Height { get; }
    public int Health { get; }
    public int MaxHealth { get; }
    public float Yaw { get; }
    public bool OnGround { get; }
    public float HurtFlash { get; }
    public float SpecialProgress { get; }
    public float Age { get; }
    public Vector4 Tint { get; }
    public bool Elite { get; }
    public EliteMobVariant EliteVariant { get; }
    public float StaggerProgress { get; }

    public MobRenderInstance(
        MobType type,
        Vector3 position,
        Vector3 velocity,
        float width,
        float height,
        int health,
        int maxHealth,
        float yaw,
        bool onGround,
        float hurtFlash,
        float specialProgress,
        float age,
        Vector4 tint,
        bool elite = false,
        EliteMobVariant eliteVariant = EliteMobVariant.None,
        float staggerProgress = 0f)
    {
        Type = type;
        Position = position;
        Velocity = velocity;
        Width = width;
        Height = height;
        Health = health;
        MaxHealth = maxHealth;
        Yaw = yaw;
        OnGround = onGround;
        HurtFlash = hurtFlash;
        SpecialProgress = specialProgress;
        Age = age;
        Tint = tint;
        Elite = elite;
        EliteVariant = eliteVariant;
        StaggerProgress = staggerProgress;
    }

    public float HealthRatio
    {
        get
        {
            if (MaxHealth <= 0)
            {
                return 0f;
            }

            return Math.Clamp(Health / (float)MaxHealth, 0f, 1f);
        }
    }
}
