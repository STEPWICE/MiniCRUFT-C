namespace MiniCRUFT.World;

public sealed class WorldEditor
{
    private readonly World _world;
    private readonly IWorldChangeQueue _changeQueue;

    public WorldEditor(World world, IWorldChangeQueue changeQueue)
    {
        _world = world;
        _changeQueue = changeQueue;
    }

    public bool SetBlock(int worldX, int worldY, int worldZ, BlockId id)
    {
        if (!_world.HasChunkAt(worldX, worldZ))
        {
            return false;
        }

        if (worldY < 0 || worldY >= Chunk.SizeY)
        {
            return false;
        }

        var existing = _world.GetBlock(worldX, worldY, worldZ);
        if (existing == id)
        {
            return false;
        }

        _world.SetBlock(worldX, worldY, worldZ, id);
        _changeQueue.Enqueue(new BlockChange(worldX, worldY, worldZ, existing, id));
        return true;
    }
}
