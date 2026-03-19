namespace MiniCRUFT.World;

public sealed class BlockDefinition
{
    public BlockId Id { get; }
    public string Name { get; }
    public bool IsSolid { get; }
    public bool IsTransparent { get; }
    public bool BlocksSkyLight { get; }
    public byte LightEmission { get; }
    public TintType TintType { get; }
    public RenderMode RenderMode { get; }
    public bool IsFoliage { get; }
    public bool IsPlaceable { get; }
    public string TextureTop { get; }
    public string TextureBottom { get; }
    public string TextureSide { get; }

    public BlockDefinition(
        BlockId id,
        string name,
        bool isSolid,
        bool isTransparent,
        string textureTop,
        string textureBottom,
        string textureSide,
        byte lightEmission = 0,
        bool? blocksSkyLight = null,
        TintType tintType = TintType.None,
        RenderMode renderMode = RenderMode.Opaque,
        bool isFoliage = false,
        bool isPlaceable = true)
    {
        Id = id;
        Name = name;
        IsSolid = isSolid;
        IsTransparent = isTransparent;
        TextureTop = textureTop;
        TextureBottom = textureBottom;
        TextureSide = textureSide;
        LightEmission = lightEmission;
        BlocksSkyLight = blocksSkyLight ?? (isSolid && !isTransparent);
        TintType = tintType;
        RenderMode = renderMode;
        IsFoliage = isFoliage;
        IsPlaceable = isPlaceable;
    }

    public string GetTextureName(BlockFace face)
    {
        return face switch
        {
            BlockFace.Top => TextureTop,
            BlockFace.Bottom => TextureBottom,
            _ => TextureSide
        };
    }
}
