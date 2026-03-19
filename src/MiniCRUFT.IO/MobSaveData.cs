using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.IO;

public readonly struct MobSaveData
{
    public MobType Type { get; }
    public Vector3 Position { get; }
    public Vector3 Velocity { get; }
    public Vector3 HomePosition { get; }
    public float Yaw { get; }
    public float WanderAngle { get; }
    public int Health { get; }
    public float AttackCooldown { get; }
    public float WanderTimer { get; }
    public float IdleTimer { get; }
    public float HurtTimer { get; }
    public float SpecialTimer { get; }
    public bool SpecialActive { get; }
    public bool OnGround { get; }
    public float StepDistance { get; }
    public float Age { get; }
    public bool Elite { get; }
    public EliteMobVariant EliteVariant { get; }

    public MobSaveData(
        MobType type,
        Vector3 position,
        Vector3 velocity,
        Vector3 homePosition,
        float yaw,
        float wanderAngle,
        int health,
        float attackCooldown,
        float wanderTimer,
        float idleTimer,
        float hurtTimer,
        float specialTimer,
        bool specialActive,
        bool onGround,
        float stepDistance,
        float age,
        bool elite = false,
        EliteMobVariant eliteVariant = EliteMobVariant.None)
    {
        Type = type;
        Position = position;
        Velocity = velocity;
        HomePosition = homePosition;
        Yaw = yaw;
        WanderAngle = wanderAngle;
        Health = health;
        AttackCooldown = attackCooldown;
        WanderTimer = wanderTimer;
        IdleTimer = idleTimer;
        HurtTimer = hurtTimer;
        SpecialTimer = specialTimer;
        SpecialActive = specialActive;
        OnGround = onGround;
        StepDistance = stepDistance;
        Age = age;
        Elite = elite;
        EliteVariant = eliteVariant;
    }
}
