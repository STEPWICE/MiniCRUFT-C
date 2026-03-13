namespace MiniCRUFT.World;

public enum BlockFace : byte
{
    NegativeX = 0,
    PositiveX = 1,
    NegativeY = 2,
    PositiveY = 3,
    NegativeZ = 4,
    PositiveZ = 5
}

public static class BlockFaceExtensions
{
    public static BlockFace Opposite(this BlockFace face)
    {
        return face switch
        {
            BlockFace.NegativeX => BlockFace.PositiveX,
            BlockFace.PositiveX => BlockFace.NegativeX,
            BlockFace.NegativeY => BlockFace.PositiveY,
            BlockFace.PositiveY => BlockFace.NegativeY,
            BlockFace.NegativeZ => BlockFace.PositiveZ,
            BlockFace.PositiveZ => BlockFace.NegativeZ,
            _ => throw new ArgumentOutOfRangeException(nameof(face))
        };
    }

    public static (int OffsetX, int OffsetY, int OffsetZ) GetOffset(this BlockFace face)
    {
        return face switch
        {
            BlockFace.NegativeX => (-1, 0, 0),
            BlockFace.PositiveX => (1, 0, 0),
            BlockFace.NegativeY => (0, -1, 0),
            BlockFace.PositiveY => (0, 1, 0),
            BlockFace.NegativeZ => (0, 0, -1),
            BlockFace.PositiveZ => (0, 0, 1),
            _ => throw new ArgumentOutOfRangeException(nameof(face))
        };
    }
}
