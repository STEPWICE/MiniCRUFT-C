using MiniCRUFT.World;

namespace MiniCRUFT.Game;

public sealed class GameHost
{
    private readonly WorldHost _world = new();
    private readonly WorldMeshingScheduler _meshingScheduler = new();

    public WorldHost World => _world;

    public void Initialize()
    {
        _world.Initialize();
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
