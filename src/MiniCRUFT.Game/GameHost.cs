using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class GameHost
{
    private WorldHost _world = new();
    private readonly WorldMeshingScheduler _meshingScheduler = new();

    public WorldHost World => _world;

    public void Initialize(int? seed = null)
    {
        _world.Initialize(seed);
    }

    public void LoadWorld(WorldHost world)
    {
        ArgumentNullException.ThrowIfNull(world);
        _world = world;
    }

    public void Update()
    {
        _meshingScheduler.ProcessDirtySubchunkSnapshots(_world);
    }

    public void Shutdown()
    {
        _world.Shutdown();
    }
}
