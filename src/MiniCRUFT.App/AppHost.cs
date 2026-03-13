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

        try
        {
            var savePath = persistence.GetDefaultSavePath();

            Console.WriteLine($"{AppInfo.ProductName} {AppInfo.VersionLabel} started.");
            Console.WriteLine($"Save file: {savePath}");
            RunFrame(game, renderer);

            while (true)
            {
                Console.WriteLine();
                Console.Write("Command [n]ew [s]ave [l]oad [u]pdate [q]uit: ");
                var command = Console.ReadLine()?.Trim().ToLowerInvariant();

                switch (command)
                {
                    case "n":
                    case "new":
                        Console.Write("Seed (blank = keep default): ");
                        Console.WriteLine(HandleCreateWorld(game, Console.ReadLine()));
                        RunFrame(game, renderer);
                        break;
                    case "s":
                    case "save":
                        Console.WriteLine(HandleSaveWorld(game, persistence));
                        break;
                    case "l":
                    case "load":
                        Console.WriteLine(HandleLoadWorld(game, persistence));
                        RunFrame(game, renderer);
                        break;
                    case "u":
                    case "update":
                        RunFrame(game, renderer);
                        Console.WriteLine($"Frame updated for seed {game.World.Seed}.");
                        break;
                    case "q":
                    case "quit":
                        return;
                    default:
                        Console.WriteLine("Unknown command. Use new, save, load, update, or quit.");
                        break;
                }
            }
        }
        finally
        {
            renderer.Shutdown();
            game.Shutdown();
            persistence.Shutdown();
            platform.Shutdown();
        }
    }

    public static string HandleCreateWorld(GameHost game, string? seedText)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (string.IsNullOrWhiteSpace(seedText))
        {
            game.CreateWorld();
            return $"Created new world with default seed {game.World.Seed}.";
        }

        if (!int.TryParse(seedText.Trim(), out var seed))
        {
            return $"Invalid seed '{seedText}'. Enter a whole number or leave it blank.";
        }

        game.CreateWorld(seed);
        return $"Created new world with seed {seed}.";
    }

    public static string HandleSaveWorld(GameHost game, PersistenceHost persistence)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(persistence);

        var savePath = persistence.GetDefaultSavePath();
        persistence.SaveWorld(savePath, game.World);
        return $"Saved world seed {game.World.Seed} to {savePath}.";
    }

    public static string HandleLoadWorld(GameHost game, PersistenceHost persistence)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(persistence);

        var savePath = persistence.GetDefaultSavePath();
        if (!persistence.SaveFileExists(savePath))
        {
            return $"No save file found at {savePath}.";
        }

        game.LoadWorld(persistence.LoadWorld(savePath));
        return $"Loaded world seed {game.World.Seed} from {savePath}.";
    }

    private static void RunFrame(GameHost game, RendererHost renderer)
    {
        game.Update();
        renderer.PickupBuiltSubchunkMeshingOutputs(game.World);
        renderer.ProcessPendingUploads();
        renderer.RenderFrame();
    }
}
