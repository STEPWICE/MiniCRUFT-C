using System;
using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.Game;

internal sealed class MobState
{
    public MobDefinition Definition { get; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Yaw { get; set; }
    public int Health { get; set; }
    public float AttackCooldown { get; set; }
    public float WanderTimer { get; set; }
    public float IdleTimer { get; set; }
    public float HurtTimer { get; set; }
    public float StaggerTimer { get; set; }
    public float SpecialTimer { get; set; }
    public bool SpecialActive { get; set; }
    public float StepDistance { get; set; }
    public float WanderAngle { get; set; }
    public bool OnGround { get; set; }
    public Vector3 HomePosition { get; }
    public float Age { get; set; }
    public float PursuitTimer { get; set; }
    public Vector3 LastKnownTargetPosition { get; set; }
    public Vector3 LastDamageSourcePosition { get; set; }
    public float RageTimer { get; set; }
    public int MaxHealth { get; }
    public bool Elite { get; }
    public EliteMobVariant EliteVariant { get; }

    public MobState(MobDefinition definition, Vector3 position, Vector3 velocity, float yaw, float wanderAngle, Vector3 homePosition, int health, int maxHealth, bool elite, EliteMobVariant eliteVariant = EliteMobVariant.None)
    {
        Definition = definition;
        Position = position;
        Velocity = velocity;
        Yaw = yaw;
        Health = health;
        WanderAngle = wanderAngle;
        HomePosition = homePosition;
        LastKnownTargetPosition = homePosition;
        MaxHealth = Math.Max(1, maxHealth);
        Elite = elite;
        EliteVariant = eliteVariant;
        LastDamageSourcePosition = homePosition;
    }

    public MobType Type => Definition.Type;
}
