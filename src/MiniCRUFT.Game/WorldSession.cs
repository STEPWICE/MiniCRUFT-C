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
    public ChunkLoadWorker Loader { get; }
    public IWorldChangeQueue ChangeQueue { get; }
    public WorldEditor Editor { get; }
    public FallingBlockSystem FallingBlocks { get; }
    public FluidSystem Fluids { get; }
    public FireSystem FireSystem { get; }
    public ChunkSaveQueue SaveQueue { get; }
    public WorldRenderer Renderer { get; }
    public MobSystem Mobs { get; }
    public TntSystem TntSystem { get; }
    public ChunkManager ChunkManager { get; }

    public WorldSession(int seed, GameConfig config, AssetStore assets, RenderDevice device)
    {
        Seed = seed;
        Settings = WorldGenSettings.FromConfig(config.WorldGen);
        World = new WorldType(seed, Settings);
        HeightSampler = new WorldHeightSampler(seed, Settings);
        Generator = new ChunkGenerationWorker(World, Math.Max(1, config.WorldGen.ChunkGenerationWorkers));
        Storage = new FileChunkStorage(config.WorldPath);
        Loader = new ChunkLoadWorker(Storage, Math.Max(1, config.SaveSettings.LoadWorkers));
        ChangeQueue = new WorldChangeQueue();
        Editor = new WorldEditor(World, ChangeQueue);
        FallingBlocks = new FallingBlockSystem(config.Falling);
        Fluids = new FluidSystem(config.Fluid);
        FireSystem = new FireSystem(seed, config.Fire);
        SaveQueue = new ChunkSaveQueue(Storage, config.SaveSettings.SaveWorkers);
        Renderer = new WorldRenderer(device, assets, config.Render, config.FirstPerson, config.Atmosphere, config.Weather, config.Particles, config.Ui, HeightSampler, Settings);
        Mobs = new MobSystem(seed, config.Mob);
        TntSystem = new TntSystem(config.Tnt);
        ChunkManager = new ChunkManager(World, Generator, Loader, Renderer, SaveQueue, config.SaveSettings);
    }

    public void Dispose()
    {
        Renderer.Dispose();
        Loader.Dispose();
        Generator.Dispose();
        SaveQueue.Dispose();
    }
}
