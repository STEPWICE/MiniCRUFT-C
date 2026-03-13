using MiniCRUFT.Game;
using MiniCRUFT.Persistence;
using MiniCRUFT.Platform;
using MiniCRUFT.Rendering;

namespace MiniCRUFT.App;

public sealed class AppHost
{
    public void Run()
    {
        var platform = new PlatformServices();
        var persistence = new PersistenceHost();
        var game = new GameHost();
        var renderer = new RendererHost();

        platform.Initialize();
        persistence.Initialize();
        game.Initialize();
        renderer.Initialize();

        Console.WriteLine($"{AppInfo.ProductName} {AppInfo.VersionLabel} skeleton started.");

        game.Update();
        renderer.PickupBuiltSubchunkMeshingOutputs(game.World);
        renderer.ProcessPendingUploads();
        renderer.RenderFrame();

        renderer.Shutdown();
        game.Shutdown();
        persistence.Shutdown();
        platform.Shutdown();
    }
}
