using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using MiniCRUFT.Core;

namespace MiniCRUFT.Renderer;

public static class MobModelCatalog
{
    private static readonly Dictionary<MobType, MobModelDefinition> Definitions = CreateDefinitions();
    private static readonly SpriteSource[] Sources =
    [
        new SpriteSource("zombie", Path.Combine("minecraft", "textures", "entity", "zombie", "zombie.png")),
        new SpriteSource("creeper", Path.Combine("minecraft", "textures", "entity", "creeper", "creeper.png")),
        new SpriteSource("cow", Path.Combine("minecraft", "textures", "entity", "cow", "cow.png")),
        new SpriteSource("sheep", Path.Combine("minecraft", "textures", "entity", "sheep", "sheep.png")),
        new SpriteSource("sheep_fur", Path.Combine("minecraft", "textures", "entity", "sheep", "sheep_fur.png")),
        new SpriteSource("chicken", Path.Combine("minecraft", "textures", "entity", "chicken.png")),
        new SpriteSource("herobrine", Path.Combine("minecraft", "mob", "char.png")),
        new SpriteSource("herobrine_eyes", Path.Combine("minecraft", "mob", "herobrine_eyes.png"))
    ];

    public static IReadOnlyDictionary<MobType, MobModelDefinition> All => Definitions;

    public static IReadOnlyList<SpriteSource> GetTextureSources()
    {
        return Sources;
    }

    public static MobModelDefinition Get(MobType type)
    {
        return Definitions[type];
    }

    public static bool TryGet(MobType type, out MobModelDefinition definition)
    {
        return Definitions.TryGetValue(type, out definition!);
    }

    private static Dictionary<MobType, MobModelDefinition> CreateDefinitions()
    {
        var definitions = new Dictionary<MobType, MobModelDefinition>
        {
            [MobType.Zombie] = new MobModelDefinition(
                MobType.Zombie,
                "zombie",
                null,
                false,
                32f,
                -MathF.PI * 0.5f,
                0.7f,
                8f,
                1.6f,
                0.65f,
                0f,
                0f,
                0f,
                0f,
                [
                    new MobBox(MobPartKind.LegBackRight, V(-4f, 0f, -2f), V(4f, 12f, 4f), U(0f, 16f), 0.82f),
                    new MobBox(MobPartKind.LegFrontRight, V(0f, 0f, -2f), V(4f, 12f, 4f), U(0f, 16f), 0.82f),
                    new MobBox(MobPartKind.Body, V(-4f, 12f, -2f), V(8f, 12f, 4f), U(16f, 16f), 0.90f),
                    new MobBox(MobPartKind.ArmRight, V(-8f, 12f, -2f), V(4f, 12f, 4f), U(40f, 16f), 0.82f),
                    new MobBox(MobPartKind.ArmLeft, V(4f, 12f, -2f), V(4f, 12f, 4f), U(40f, 16f), 0.82f),
                    new MobBox(MobPartKind.Head, V(-4f, 24f, -4f), V(8f, 8f, 8f), U(0f, 0f), 0.95f)
                ]),

            [MobType.Creeper] = new MobModelDefinition(
                MobType.Creeper,
                "creeper",
                null,
                false,
                26f,
                -MathF.PI * 0.5f,
                0.55f,
                8f,
                1.35f,
                0.55f,
                0f,
                0f,
                0.12f,
                0f,
                [
                    new MobBox(MobPartKind.LegBackLeft, V(-4f, 0f, -4f), V(4f, 6f, 4f), U(0f, 16f), 0.80f),
                    new MobBox(MobPartKind.LegBackRight, V(0f, 0f, -4f), V(4f, 6f, 4f), U(0f, 16f), 0.80f),
                    new MobBox(MobPartKind.LegFrontLeft, V(-4f, 0f, 0f), V(4f, 6f, 4f), U(0f, 16f), 0.80f),
                    new MobBox(MobPartKind.LegFrontRight, V(0f, 0f, 0f), V(4f, 6f, 4f), U(0f, 16f), 0.80f),
                    new MobBox(MobPartKind.Body, V(-4f, 6f, -2f), V(8f, 12f, 4f), U(16f, 8f), 0.90f),
                    new MobBox(MobPartKind.Head, V(-4f, 18f, -4f), V(8f, 8f, 8f), U(0f, 0f), 0.95f)
                ]),

            [MobType.Cow] = new MobModelDefinition(
                MobType.Cow,
                "cow",
                null,
                false,
                18f,
                -MathF.PI * 0.5f,
                0.45f,
                7.5f,
                0.95f,
                0.38f,
                0f,
                0f,
                0f,
                0f,
                [
                    new MobBox(MobPartKind.LegBackLeft, V(-6f, 0f, -4f), V(4f, 6f, 4f), U(0f, 16f), 0.78f),
                    new MobBox(MobPartKind.LegBackRight, V(2f, 0f, -4f), V(4f, 6f, 4f), U(0f, 16f), 0.78f),
                    new MobBox(MobPartKind.LegFrontLeft, V(-6f, 0f, 0f), V(4f, 6f, 4f), U(0f, 16f), 0.78f),
                    new MobBox(MobPartKind.LegFrontRight, V(2f, 0f, 0f), V(4f, 6f, 4f), U(0f, 16f), 0.78f),
                    new MobBox(MobPartKind.Body, V(-6f, 6f, -3f), V(12f, 8f, 6f), U(28f, 8f), 0.94f),
                    new MobBox(MobPartKind.Head, V(-4f, 10f, 3f), V(8f, 8f, 6f), U(0f, 0f), 0.95f)
                ]),

            [MobType.Sheep] = new MobModelDefinition(
                MobType.Sheep,
                "sheep",
                "sheep_fur",
                false,
                18f,
                -MathF.PI * 0.5f,
                0.42f,
                7.5f,
                0.95f,
                0.38f,
                0f,
                0f,
                0f,
                1.25f,
                [
                    new MobBox(MobPartKind.LegBackLeft, V(-6f, 0f, -4f), V(4f, 6f, 4f), U(0f, 16f), 0.82f),
                    new MobBox(MobPartKind.LegBackRight, V(2f, 0f, -4f), V(4f, 6f, 4f), U(0f, 16f), 0.82f),
                    new MobBox(MobPartKind.LegFrontLeft, V(-6f, 0f, 0f), V(4f, 6f, 4f), U(0f, 16f), 0.82f),
                    new MobBox(MobPartKind.LegFrontRight, V(2f, 0f, 0f), V(4f, 6f, 4f), U(0f, 16f), 0.82f),
                    new MobBox(MobPartKind.Body, V(-6f, 6f, -3f), V(12f, 8f, 6f), U(28f, 8f), 0.98f),
                    new MobBox(MobPartKind.Head, V(-4f, 10f, 3f), V(8f, 8f, 6f), U(0f, 0f), 0.98f)
                ]),

            [MobType.Chicken] = new MobModelDefinition(
                MobType.Chicken,
                "chicken",
                null,
                false,
                15f,
                -MathF.PI * 0.5f,
                0.55f,
                9f,
                0.85f,
                0.75f,
                12f,
                0.85f,
                0f,
                0f,
                [
                    new MobBox(MobPartKind.LegBackLeft, V(-2f, 0f, -1f), V(2f, 6f, 2f), U(0f, 16f), 0.75f),
                    new MobBox(MobPartKind.LegBackRight, V(0f, 0f, -1f), V(2f, 6f, 2f), U(0f, 16f), 0.75f),
                    new MobBox(MobPartKind.Body, V(-3f, 6f, -3f), V(6f, 6f, 6f), U(14f, 4f), 0.94f),
                    new MobBox(MobPartKind.WingLeft, V(-4f, 7f, -1.5f), V(1f, 4f, 3f), U(24f, 13f), 0.86f),
                    new MobBox(MobPartKind.WingRight, V(3f, 7f, -1.5f), V(1f, 4f, 3f), U(24f, 13f), 0.86f),
                    new MobBox(MobPartKind.Head, V(-2f, 9f, 2f), V(4f, 6f, 3f), U(0f, 0f), 0.98f),
                    new MobBox(MobPartKind.Beak, V(-1f, 11f, 5f), V(2f, 2f, 1f), U(14f, 0f), 0.95f)
                ])
        };

        if (Enum.TryParse("Herobrine", false, out MobType herobrineType))
        {
            definitions[herobrineType] = new MobModelDefinition(
                herobrineType,
                "herobrine",
                "herobrine_eyes",
                true,
                32f,
                -MathF.PI * 0.5f,
                0.62f,
                8f,
                1.55f,
                0.62f,
                0f,
                0f,
                0f,
                0.18f,
                [
                    new MobBox(MobPartKind.LegBackRight, V(-4f, 0f, -2f), V(4f, 12f, 4f), U(0f, 16f), 0.84f),
                    new MobBox(MobPartKind.LegFrontRight, V(0f, 0f, -2f), V(4f, 12f, 4f), U(0f, 16f), 0.84f),
                    new MobBox(MobPartKind.Body, V(-4f, 12f, -2f), V(8f, 12f, 4f), U(16f, 16f), 0.92f),
                    new MobBox(MobPartKind.ArmRight, V(-8f, 12f, -2f), V(4f, 12f, 4f), U(40f, 16f), 0.84f),
                    new MobBox(MobPartKind.ArmLeft, V(4f, 12f, -2f), V(4f, 12f, 4f), U(40f, 16f), 0.84f),
                    new MobBox(MobPartKind.Head, V(-4f, 24f, -4f), V(8f, 8f, 8f), U(0f, 0f), 0.98f)
                ]);
        }

        return definitions;
    }

    private static Vector3 V(float x, float y, float z)
    {
        return new Vector3(x, y, z);
    }

    private static Vector2 U(float x, float y)
    {
        return new Vector2(x, y);
    }
}
