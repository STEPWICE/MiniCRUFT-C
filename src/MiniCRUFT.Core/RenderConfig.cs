namespace MiniCRUFT.Core;

public sealed class RenderConfig
{
    public int Anisotropy { get; set; } = 1;
    public bool UseMipmaps { get; set; } = false;
    public float CutoutAlphaThreshold { get; set; } = 0.15f;
    public float FogStart { get; set; } = 120f;
    public float FogEnd { get; set; } = 420f;
    public bool LinearFog { get; set; } = true;
    public float AmbientOcclusionMin { get; set; } = 0.6f;
    public float AmbientOcclusionStrength { get; set; } = 0.0f;
    public float BiomeTintStrength { get; set; } = 0.45f;
    public float MinLight { get; set; } = 0.2f;
    public float SunLightMin { get; set; } = 0.3f;
    public int MaxMeshUploadsPerFrame { get; set; } = 4;
    public const int DefaultChunkMeshWorkers = 2;
    public int ChunkMeshWorkers { get; set; } = DefaultChunkMeshWorkers;
    public FaceShadingConfig FaceShading { get; set; } = new();
    public PaletteConfig Palette { get; set; } = new();
    public FogConfig Fog { get; set; } = new();
    public FoliageConfig Foliage { get; set; } = new();
    public LodConfig Lod { get; set; } = new();
}
