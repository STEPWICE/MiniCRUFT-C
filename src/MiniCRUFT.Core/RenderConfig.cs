namespace MiniCRUFT.Core;

public sealed class RenderConfig
{
    public int Anisotropy { get; set; } = 4;
    public bool UseMipmaps { get; set; } = true;
    public float CutoutAlphaThreshold { get; set; } = 0.35f;
    public float FogStart { get; set; } = 120f;
    public float FogEnd { get; set; } = 420f;
    public bool LinearFog { get; set; } = true;
    public float AmbientOcclusionMin { get; set; } = 0.6f;
    public float AmbientOcclusionStrength { get; set; } = 1.0f;
    public float BiomeTintStrength { get; set; } = 0.45f;
    public float MinLight { get; set; } = 0.2f;
    public float SunLightMin { get; set; } = 0.3f;
    public int MaxMeshUploadsPerFrame { get; set; } = 4;
    public FogConfig Fog { get; set; } = new();
    public FoliageConfig Foliage { get; set; } = new();
    public LodConfig Lod { get; set; } = new();
}
