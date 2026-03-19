using System;

namespace MiniCRUFT.World;

public sealed class WorldGenSettings
{
    public bool StrictBetaMode { get; set; }
    public int BaseHeight { get; set; } = 52;
    public int SeaLevel { get; set; } = 62;
    public float ContinentalAmplitude { get; set; } = 38f;
    public float PeakAmplitude { get; set; } = 18f;
    public float ErosionAmplitude { get; set; } = 12f;
    public float RidgeAmplitude { get; set; } = 7f;
    public float RiverThreshold { get; set; } = 0.04f;
    public float RiverWidth { get; set; } = 2.2f;
    public int RiverDepth { get; set; } = 3;
    public float RiverScale { get; set; } = 0.75f;
    public float RiverWarpStrength { get; set; } = 150f;
    public float RiverInfluenceWidth { get; set; } = 1.35f;
    public float RiverBankInfluenceMin { get; set; } = 0.25f;
    public float RiverWaterInfluenceMin { get; set; } = 0.6f;
    public float CaveThreshold { get; set; } = 0.63f;
    public bool GenerateAquifers { get; set; } = true;
    public int AquiferWaterLevelOffset { get; set; } = 10;
    public float AquiferWaterScale { get; set; } = 0.06f;
    public float AquiferWaterThreshold { get; set; } = 0.66f;
    public int AquiferLavaLevelOffset { get; set; } = 28;
    public float AquiferLavaScale { get; set; } = 0.045f;
    public float AquiferLavaThreshold { get; set; } = 0.74f;
    public float BiomeScale { get; set; } = 0.38f;
    public float BiomeWarpStrength { get; set; } = 120f;
    public float BiomeContrast { get; set; } = 1.08f;
    public float MountainBiomeThreshold { get; set; } = 0.86f;
    public float WaterSlopeThreshold { get; set; } = 5f;
    public int WaterMinDepth { get; set; } = 1;
    public float ForestHeightScale { get; set; } = 0.92f;
    public float ForestHeightBias { get; set; } = 1f;
    public float PlainsHeightScale { get; set; } = 0.86f;
    public float PlainsHeightBias { get; set; } = -1f;
    public float DesertHeightScale { get; set; } = 0.84f;
    public float DesertHeightBias { get; set; } = -1f;
    public float MountainsHeightScale { get; set; } = 1.1f;
    public float MountainsHeightBias { get; set; } = 6f;
    public float TaigaHeightScale { get; set; } = 0.92f;
    public float TaigaHeightBias { get; set; } = 0f;
    public float TundraHeightScale { get; set; } = 0.88f;
    public float TundraHeightBias { get; set; } = -1f;
    public float SwampHeightScale { get; set; } = 0.75f;
    public float SwampHeightBias { get; set; } = -3f;
    public float SavannaHeightScale { get; set; } = 0.9f;
    public float SavannaHeightBias { get; set; } = 0f;
    public float ShrublandHeightScale { get; set; } = 0.85f;
    public float ShrublandHeightBias { get; set; } = -1f;
    public int MountainStoneHeightOffset { get; set; } = 16;
    public float MountainStoneSlope { get; set; } = 6.5f;
    public int SnowLine { get; set; } = 90;
    public float DetailScale { get; set; } = 0.95f;
    public float ForestDetailAmplitude { get; set; } = 1.0f;
    public float PlainsDetailAmplitude { get; set; } = 0.9f;
    public float DesertDetailAmplitude { get; set; } = 1.6f;
    public float MountainsDetailAmplitude { get; set; } = 0.9f;
    public float TaigaDetailAmplitude { get; set; } = 1.0f;
    public float TundraDetailAmplitude { get; set; } = 0.6f;
    public float SwampDetailAmplitude { get; set; } = 0.45f;
    public float SavannaDetailAmplitude { get; set; } = 0.8f;
    public float ShrublandDetailAmplitude { get; set; } = 0.8f;
    public float DesertDuneScale { get; set; } = 0.9f;
    public float DesertDuneAmplitude { get; set; } = 2.5f;
    public float TrailScale { get; set; } = 0.7f;
    public float TrailThreshold { get; set; } = 0.035f;
    public float TrailDirtChance { get; set; } = 0.6f;
    public float TrailClearChance { get; set; } = 0.7f;
    public float TrailSecondaryScale { get; set; } = 0.25f;
    public float TrailSecondaryThreshold { get; set; } = 0.25f;
    public float TreeClusterScale { get; set; } = 0.75f;
    public float TreeClusterStrength { get; set; } = 0.55f;
    public float TreeChance { get; set; } = 0.05f;
    public float ForestTreeChance { get; set; } = 0.055f;
    public float ForestTallGrassChance { get; set; } = 0.05f;
    public float PlainsTreeChance { get; set; } = 0.018f;
    public float TaigaTallGrassChance { get; set; } = 0.03f;
    public float TaigaTreeChance { get; set; } = 0.05f;
    public float TundraTreeChance { get; set; } = 0.002f;
    public float TundraTallGrassChance { get; set; } = 0.01f;
    public float SavannaTreeChance { get; set; } = 0.014f;
    public float ShrublandTreeChance { get; set; } = 0.02f;
    public float SwampTreeChance { get; set; } = 0.045f;
    public float MountainsTreeChance { get; set; } = 0.015f;
    public float PlainsTallGrassChance { get; set; } = 0.12f;
    public float SavannaTallGrassChance { get; set; } = 0.18f;
    public float ShrublandTallGrassChance { get; set; } = 0.12f;
    public float ForestFlowerChance { get; set; } = 0.045f;
    public float SwampFlowerChance { get; set; } = 0.02f;
    public float DesertCactusChance { get; set; } = 0.05f;
    public float DesertDeadBushChance { get; set; } = 0.08f;
    public float SugarCaneChance { get; set; } = 0.12f;
    public float LargeTreeChance { get; set; } = 0.1f;
    public int LargeTreeMinHeight { get; set; } = 8;
    public int LargeTreeMaxHeight { get; set; } = 12;
    public int LargeTreeLeafRadius { get; set; } = 4;
    public int LargeTreeCanopyDepth { get; set; } = 4;
    public float TreeMaxSlope { get; set; } = 6.5f;
    public BiomeId? ForcedBiome { get; set; }
    public float CliffSlopeThreshold { get; set; } = 10f;
    public float CliffSmoothStrength { get; set; } = 0.85f;
    public float RidgeClamp { get; set; } = 0.6f;
    public int BeachSize { get; set; } = 5;

    public float PondChance { get; set; } = 0.01f;
    public int PondRadiusMin { get; set; } = 2;
    public int PondRadiusMax { get; set; } = 4;
    public int PondDepth { get; set; } = 3;
    public float BoulderChance { get; set; } = 0.01f;
    public int BoulderRadiusMin { get; set; } = 2;
    public int BoulderRadiusMax { get; set; } = 3;
    public float BoulderStoneChance { get; set; } = 0.8f;
    public float FallenLogChance { get; set; } = 0.015f;
    public int FallenLogMinLength { get; set; } = 3;
    public int FallenLogMaxLength { get; set; } = 6;
    public float FlowerPatchChance { get; set; } = 0.02f;
    public int FlowerPatchRadiusMin { get; set; } = 2;
    public int FlowerPatchRadiusMax { get; set; } = 4;
    public float GravelPatchChance { get; set; } = 0.01f;
    public int GravelPatchRadiusMin { get; set; } = 2;
    public int GravelPatchRadiusMax { get; set; } = 3;
    public float StructureChance { get; set; } = 0.012f;
    public float CampChance { get; set; } = 0.0045f;
    public float WatchtowerChance { get; set; } = 0.0025f;
    public float BuriedCacheChance { get; set; } = 0.003f;
    public float CaveCacheChance { get; set; } = 0.002f;
    public float RuinChance { get; set; } = 0.0024f;
    public float MineShaftChance { get; set; } = 0.0016f;
    public int StructureMargin { get; set; } = 4;

    public static WorldGenSettings FromConfig(MiniCRUFT.Core.WorldGenConfig config)
    {
        return new WorldGenSettings
        {
            StrictBetaMode = config.StrictBetaMode,
            BaseHeight = config.BaseHeight,
            SeaLevel = config.SeaLevel,
            ContinentalAmplitude = config.ContinentalAmplitude,
            PeakAmplitude = config.PeakAmplitude,
            ErosionAmplitude = config.ErosionAmplitude,
            RidgeAmplitude = config.RidgeAmplitude,
            RiverThreshold = config.RiverThreshold,
            RiverWidth = config.RiverWidth,
            RiverDepth = config.RiverDepth,
            RiverScale = config.RiverScale,
            RiverWarpStrength = config.RiverWarpStrength,
            RiverInfluenceWidth = config.RiverInfluenceWidth,
            RiverBankInfluenceMin = config.RiverBankInfluenceMin,
            RiverWaterInfluenceMin = config.RiverWaterInfluenceMin,
            CaveThreshold = config.CaveThreshold,
            GenerateAquifers = config.GenerateAquifers,
            AquiferWaterLevelOffset = config.AquiferWaterLevelOffset,
            AquiferWaterScale = config.AquiferWaterScale,
            AquiferWaterThreshold = config.AquiferWaterThreshold,
            AquiferLavaLevelOffset = config.AquiferLavaLevelOffset,
            AquiferLavaScale = config.AquiferLavaScale,
            AquiferLavaThreshold = config.AquiferLavaThreshold,
            BiomeScale = config.BiomeScale,
            BiomeWarpStrength = config.BiomeWarpStrength,
            BiomeContrast = config.BiomeContrast,
            MountainBiomeThreshold = config.MountainBiomeThreshold,
            WaterSlopeThreshold = config.WaterSlopeThreshold,
            WaterMinDepth = config.WaterMinDepth,
            ForestHeightScale = config.ForestHeightScale,
            ForestHeightBias = config.ForestHeightBias,
            PlainsHeightScale = config.PlainsHeightScale,
            PlainsHeightBias = config.PlainsHeightBias,
            DesertHeightScale = config.DesertHeightScale,
            DesertHeightBias = config.DesertHeightBias,
            MountainsHeightScale = config.MountainsHeightScale,
            MountainsHeightBias = config.MountainsHeightBias,
            TaigaHeightScale = config.TaigaHeightScale,
            TaigaHeightBias = config.TaigaHeightBias,
            TundraHeightScale = config.TundraHeightScale,
            TundraHeightBias = config.TundraHeightBias,
            SwampHeightScale = config.SwampHeightScale,
            SwampHeightBias = config.SwampHeightBias,
            SavannaHeightScale = config.SavannaHeightScale,
            SavannaHeightBias = config.SavannaHeightBias,
            ShrublandHeightScale = config.ShrublandHeightScale,
            ShrublandHeightBias = config.ShrublandHeightBias,
            MountainStoneHeightOffset = config.MountainStoneHeightOffset,
            MountainStoneSlope = config.MountainStoneSlope,
            SnowLine = config.SnowLine,
            DetailScale = config.DetailScale,
            ForestDetailAmplitude = config.ForestDetailAmplitude,
            PlainsDetailAmplitude = config.PlainsDetailAmplitude,
            DesertDetailAmplitude = config.DesertDetailAmplitude,
            MountainsDetailAmplitude = config.MountainsDetailAmplitude,
            TaigaDetailAmplitude = config.TaigaDetailAmplitude,
            TundraDetailAmplitude = config.TundraDetailAmplitude,
            SwampDetailAmplitude = config.SwampDetailAmplitude,
            SavannaDetailAmplitude = config.SavannaDetailAmplitude,
            ShrublandDetailAmplitude = config.ShrublandDetailAmplitude,
            DesertDuneScale = config.DesertDuneScale,
            DesertDuneAmplitude = config.DesertDuneAmplitude,
            TrailScale = config.TrailScale,
            TrailThreshold = config.TrailThreshold,
            TrailDirtChance = config.TrailDirtChance,
            TrailClearChance = config.TrailClearChance,
            TrailSecondaryScale = config.TrailSecondaryScale,
            TrailSecondaryThreshold = config.TrailSecondaryThreshold,
            TreeClusterScale = config.TreeClusterScale,
            TreeClusterStrength = config.TreeClusterStrength,
            TreeChance = config.TreeChance,
            ForestTreeChance = config.ForestTreeChance,
            ForestTallGrassChance = config.ForestTallGrassChance,
            PlainsTreeChance = config.PlainsTreeChance,
            TaigaTallGrassChance = config.TaigaTallGrassChance,
            TaigaTreeChance = config.TaigaTreeChance,
            TundraTreeChance = config.TundraTreeChance,
            TundraTallGrassChance = config.TundraTallGrassChance,
            SavannaTreeChance = config.SavannaTreeChance,
            ShrublandTreeChance = config.ShrublandTreeChance,
            SwampTreeChance = config.SwampTreeChance,
            MountainsTreeChance = config.MountainsTreeChance,
            PlainsTallGrassChance = config.PlainsTallGrassChance,
            SavannaTallGrassChance = config.SavannaTallGrassChance,
            ShrublandTallGrassChance = config.ShrublandTallGrassChance,
            ForestFlowerChance = config.ForestFlowerChance,
            SwampFlowerChance = config.SwampFlowerChance,
            DesertCactusChance = config.DesertCactusChance,
            DesertDeadBushChance = config.DesertDeadBushChance,
            SugarCaneChance = config.SugarCaneChance,
            LargeTreeChance = config.LargeTreeChance,
            LargeTreeMinHeight = config.LargeTreeMinHeight,
            LargeTreeMaxHeight = config.LargeTreeMaxHeight,
            LargeTreeLeafRadius = config.LargeTreeLeafRadius,
            LargeTreeCanopyDepth = config.LargeTreeCanopyDepth,
            TreeMaxSlope = config.TreeMaxSlope,
            PondChance = config.PondChance,
            PondRadiusMin = config.PondRadiusMin,
            PondRadiusMax = config.PondRadiusMax,
            PondDepth = config.PondDepth,
            BoulderChance = config.BoulderChance,
            BoulderRadiusMin = config.BoulderRadiusMin,
            BoulderRadiusMax = config.BoulderRadiusMax,
            BoulderStoneChance = config.BoulderStoneChance,
            FallenLogChance = config.FallenLogChance,
            FallenLogMinLength = config.FallenLogMinLength,
            FallenLogMaxLength = config.FallenLogMaxLength,
            FlowerPatchChance = config.FlowerPatchChance,
            FlowerPatchRadiusMin = config.FlowerPatchRadiusMin,
            FlowerPatchRadiusMax = config.FlowerPatchRadiusMax,
            GravelPatchChance = config.GravelPatchChance,
            GravelPatchRadiusMin = config.GravelPatchRadiusMin,
            GravelPatchRadiusMax = config.GravelPatchRadiusMax,
            StructureChance = config.StructureChance,
            CampChance = config.CampChance,
            WatchtowerChance = config.WatchtowerChance,
            BuriedCacheChance = config.BuriedCacheChance,
            CaveCacheChance = config.CaveCacheChance,
            RuinChance = config.RuinChance,
            MineShaftChance = config.MineShaftChance,
            StructureMargin = config.StructureMargin,
            ForcedBiome = ParseForcedBiome(config.ForcedBiome),
            CliffSlopeThreshold = config.CliffSlopeThreshold,
            CliffSmoothStrength = config.CliffSmoothStrength,
            RidgeClamp = config.RidgeClamp,
            BeachSize = config.BeachSize
        };
    }

    private static BiomeId? ParseForcedBiome(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("Any", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Enum.TryParse<BiomeId>(value, true, out var biome) ? biome : null;
    }
}
