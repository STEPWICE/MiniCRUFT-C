using System;
using MiniCRUFT.Core;
using MiniCRUFT.IO;
using MiniCRUFT.Renderer;
using MiniCRUFT.World;
using WorldType = MiniCRUFT.World.World;

namespace MiniCRUFT.Game;

public sealed class WorldSession : IDisposable
{
    public int Seed { get; }
    public WorldGenSettings Settings { get; }
    public WorldType World { get; }
    public WorldHeightSampler HeightSampler { get; }
    public ChunkGenerationWorker Generator { get; }
    public FileChunkStorage Storage { get; }
    public IWorldChangeQueue ChangeQueue { get; }
    public WorldEditor Editor { get; }
    public ChunkSaveQueue SaveQueue { get; }
    public WorldRenderer Renderer { get; }
    public ChunkManager ChunkManager { get; }

    public WorldSession(int seed, GameConfig config, AssetStore assets, RenderDevice device)
    {
        Seed = seed;
        Settings = WorldGenSettings.FromConfig(config.WorldGen);
        World = new WorldType(seed, Settings);
        HeightSampler = new WorldHeightSampler(seed, Settings);
        Generator = new ChunkGenerationWorker(World, Math.Max(1, Environment.ProcessorCount / 2));
        Storage = new FileChunkStorage(config.WorldPath);
        ChangeQueue = new WorldChangeQueue();
        Editor = new WorldEditor(World, ChangeQueue);
        SaveQueue = new ChunkSaveQueue(Storage, config.SaveSettings.SaveWorkers);
        Renderer = new WorldRenderer(device, assets, config.Render, config.Atmosphere, config.Ui, HeightSampler, Settings);
        ChunkManager = new ChunkManager(World, Generator, Storage, Renderer, SaveQueue, config.SaveSettings);
    }

    public void Dispose()
    {
        Renderer.Dispose();
        Generator.Dispose();
        SaveQueue.Dispose();
    }
}
