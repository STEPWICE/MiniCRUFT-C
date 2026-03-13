using MiniCRUFT.App;
using MiniCRUFT.Game;
using MiniCRUFT.Persistence;
using MiniCRUFT.Rendering;
using MiniCRUFT.World;
using Xunit;

namespace MiniCRUFT.World.Tests;

public sealed class WorldHostTests
{
    [Fact]
    public void SameSeedChunkGenerationStaysDeterministic()
    {
        var firstHost = new WorldHost();
        var secondHost = new WorldHost();
        firstHost.Initialize(12345);
        secondHost.Initialize(12345);

        foreach (var (x, z) in new[] { (0, 0), (1, 2), (-1, 0), (-3, 4), (7, -5) })
        {
            var firstChunk = firstHost.GetOrCreateChunk(x, z);
            var sameChunkInstance = firstHost.GetOrCreateChunk(x, z);
            var secondChunk = secondHost.GetOrCreateChunk(x, z);

            Assert.True(ReferenceEquals(firstChunk, sameChunkInstance), $"Expected cached chunk instance for ({x}, {z}).");
            AssertChunksEqual(firstChunk, secondChunk);
        }
    }

    [Fact]
    public void WorldSnapshotExportImportRoundtripsSeedChunkStateAndBlockData()
    {
        var source = new WorldHost();
        source.Initialize(5560);

        var westChunk = source.GetOrCreateChunk(-1, 0);
        var originChunk = source.GetOrCreateChunk(0, 0);
        westChunk.GetSubchunk(0).Fill(BlockId.Air);
        originChunk.GetSubchunk(0).Fill(BlockId.Air);
        source.SetBlock(-1, 5, 0, BlockId.Stone);
        source.SetBlock(1, 6, 1, BlockId.Dirt);

        var snapshot = source.ExportSnapshot();
        var restored = new WorldHost();
        restored.ImportSnapshot(snapshot);

        Assert.Equal(5560, restored.Seed);
        Assert.Equal(source.LoadedChunkCount, restored.LoadedChunkCount);
        Assert.Equal(source.GeneratedChunkCount, restored.GeneratedChunkCount);
        Assert.True(restored.TryGetChunk(-1, 0, out var restoredWestChunk));
        Assert.True(restored.TryGetChunk(0, 0, out var restoredOriginChunk));
        Assert.NotNull(restoredWestChunk);
        Assert.NotNull(restoredOriginChunk);
        AssertChunksEqual(westChunk, restoredWestChunk!);
        AssertChunksEqual(originChunk, restoredOriginChunk!);
        Assert.Equal(BlockId.Stone, restored.GetBlock(-1, 5, 0));
        Assert.Equal(BlockId.Dirt, restored.GetBlock(1, 6, 1));
        Assert.True(restoredWestChunk!.GetSubchunk(0).IsDirty);
        Assert.Equal(SubchunkMeshState.NeedsBuild, restoredWestChunk.GetSubchunk(0).MeshState);
    }

    [Fact]
    public void PersistenceHostRoundtripsWorldSnapshotThroughJsonStorage()
    {
        var world = new WorldHost();
        world.Initialize(5561);

        var chunk = world.GetOrCreateChunk(2, -3);
        chunk.GetSubchunk(0).Fill(BlockId.Air);
        world.SetBlock(32, 7, -48, BlockId.Grass);
        world.SetBlock(33, 8, -47, BlockId.Stone);

        var persistence = new PersistenceHost();
        persistence.Initialize();

        var directory = Path.Combine(Path.GetTempPath(), $"minicruft-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var filename = Path.Combine(directory, "world.json");

        try
        {
            persistence.SaveWorld(filename, world);
            var restored = persistence.LoadWorld(filename);

            Assert.True(File.Exists(filename));
            var savedJson = File.ReadAllText(filename);
            Assert.Contains("\"FormatVersion\": 1", savedJson);
            Assert.Contains("\"Seed\": 5561", savedJson);
            Assert.Equal(5561, restored.Seed);
            Assert.Equal(world.LoadedChunkCount, restored.LoadedChunkCount);
            Assert.Equal(BlockId.Grass, restored.GetBlock(32, 7, -48));
            Assert.Equal(BlockId.Stone, restored.GetBlock(33, 8, -47));
            Assert.True(restored.TryGetChunk(2, -3, out var restoredChunk));
            Assert.NotNull(restoredChunk);
            AssertChunksEqual(chunk, restoredChunk!);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }

            persistence.Shutdown();
        }
    }

    [Fact]
    public void SnapshotOnlyPersistsPlayerEditedChunksAndRequiredSeamNeighbors()
    {
        var world = new WorldHost();
        world.Initialize(5562);

        _ = world.GetOrCreateChunk(4, 4);
        world.GetOrCreateChunk(0, 0).GetSubchunk(1).Fill(BlockId.Air);

        world.SetBlock(15, 16, 15, BlockId.Stone);

        var snapshot = world.ExportSnapshot();
        var persistedCoordinates = snapshot.Chunks.Select(chunk => (chunk.X, chunk.Z)).ToList();

        Assert.Equal(3, snapshot.Chunks.Count);
        Assert.Equal((0, 0), persistedCoordinates[0]);
        Assert.Equal((0, 1), persistedCoordinates[1]);
        Assert.Equal((1, 0), persistedCoordinates[2]);
        Assert.DoesNotContain((4, 4), persistedCoordinates);
    }

    [Fact]
    public void PersistenceHostRestoresPlayerEditsBackIntoDirtyMeshingFlow()
    {
        var world = new WorldHost();
        world.Initialize(5563);

        var originChunk = world.GetOrCreateChunk(0, 0);
        var eastChunk = world.GetOrCreateChunk(1, 0);
        var southChunk = world.GetOrCreateChunk(0, 1);

        foreach (var chunk in new[] { originChunk, eastChunk, southChunk })
        {
            chunk.GetSubchunk(0).Fill(BlockId.Air);
            chunk.GetSubchunk(1).Fill(BlockId.Air);

            foreach (var subchunk in chunk.Subchunks)
            {
                subchunk.MarkMeshBuilt();
            }
        }

        world.SetBlock(15, 16, 15, BlockId.Stone);

        var persistence = new PersistenceHost();
        persistence.Initialize();

        try
        {
            var restored = persistence.LoadWorldFromJson(persistence.SaveWorldToJson(world));
            var scheduler = new WorldMeshingScheduler();

            Assert.Equal(BlockId.Stone, restored.GetBlock(15, 16, 15));
            Assert.Equal(3, restored.LoadedChunkCount);

            var dirty = restored.EnumerateDirtySubchunks()
                .Select(entry => (entry.Chunk.Coordinate, entry.SubchunkIndex))
                .ToList();

            Assert.Equal(24, dirty.Count);
            Assert.Contains((new ChunkCoordinate(0, 0), 0), dirty);
            Assert.Contains((new ChunkCoordinate(0, 0), 1), dirty);
            Assert.Contains((new ChunkCoordinate(0, 1), 1), dirty);
            Assert.Contains((new ChunkCoordinate(1, 0), 1), dirty);

            Assert.Equal(24, scheduler.ProcessDirtySubchunkSnapshots(restored, 24));

            var builtOutputs = restored.EnumerateBuiltSubchunkMeshingOutputs()
                .Select(output => (output.ChunkCoordinate, output.SubchunkIndex))
                .ToList();

            Assert.Equal(dirty, builtOutputs);
        }
        finally
        {
            persistence.Shutdown();
        }
    }

    [Fact]
    public void PersistenceHostLoadsLegacyUnversionedJsonByMigratingToCurrentFormat()
    {
        const string legacyJson = """
            {
              "Seed": 777,
              "Chunks": []
            }
            """;

        var persistence = new PersistenceHost();
        persistence.Initialize();

        try
        {
            var restored = persistence.LoadWorldFromJson(legacyJson);
            Assert.Equal(777, restored.Seed);
            Assert.Equal(0, restored.LoadedChunkCount);
        }
        finally
        {
            persistence.Shutdown();
        }
    }

    [Fact]
    public void PersistenceHostRejectsUnsupportedFutureSnapshotFormatVersion()
    {
        const string futureJson = """
            {
              "FormatVersion": 999,
              "Seed": 777,
              "Chunks": []
            }
            """;

        var persistence = new PersistenceHost();
        persistence.Initialize();

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() => persistence.LoadWorldFromJson(futureJson));
            Assert.Contains("unsupported", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            persistence.Shutdown();
        }
    }

    [Fact]
    public void PersistenceHostRejectsMalformedWorldSaveJson()
    {
        var persistence = new PersistenceHost();
        persistence.Initialize();

        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() => persistence.LoadWorldFromJson("{ not valid json"));
            Assert.Contains("malformed", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            persistence.Shutdown();
        }
    }

    [Fact]
    public void WorldImportRejectsCorruptSubchunkBlockPayload()
    {
        var corruptSnapshot = new WorldSaveSnapshot(
            PersistenceHost.CurrentSaveFormatVersion,
            888,
            new[]
            {
                new ChunkSaveSnapshot(
                    0,
                    0,
                    ChunkState.Generated,
                    CreateValidSubchunkSnapshots().Select((subchunk, index) => index == 0
                        ? new SubchunkSaveSnapshot(new[] { BlockId.Stone })
                        : subchunk)
                        .ToArray())
            });

        var world = new WorldHost();
        var exception = Assert.Throws<InvalidOperationException>(() => world.ImportSnapshot(corruptSnapshot));
        Assert.Contains("expected", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NegativeCoordinateMappingResolvesChunkAndLocalBlockCoordinates()
    {
        Assert.Equal(new ChunkCoordinate(-1, -1), ChunkCoordinate.FromWorldBlock(-1, -1));
        Assert.Equal(new ChunkCoordinate(-1, 0), ChunkCoordinate.FromWorldBlock(-16, 0));
        Assert.Equal(new ChunkCoordinate(-2, -2), ChunkCoordinate.FromWorldBlock(-17, -17));

        AssertLocal(LocalBlockCoordinate.FromWorldBlock(-1, 12, -1), 15, 12, 15);
        AssertLocal(LocalBlockCoordinate.FromWorldBlock(-16, 7, 0), 0, 7, 0);
        AssertLocal(LocalBlockCoordinate.FromWorldBlock(-17, 3, -17), 15, 3, 15);
        AssertLocal(LocalBlockCoordinate.FromWorldBlock(16, 9, 16), 0, 9, 0);
    }

    [Fact]
    public void WorldHostWorldSpaceReadsAndWritesCrossChunkBoundaries()
    {
        var host = new WorldHost();
        host.Initialize(9876);

        var samples = new[]
        {
            (WorldX: 15, WorldY: 20, WorldZ: 15, Block: BlockId.Stone, Chunk: new ChunkCoordinate(0, 0), LocalX: 15, LocalZ: 15),
            (WorldX: 16, WorldY: 21, WorldZ: 16, Block: BlockId.Dirt, Chunk: new ChunkCoordinate(1, 1), LocalX: 0, LocalZ: 0),
            (WorldX: -1, WorldY: 22, WorldZ: -1, Block: BlockId.Grass, Chunk: new ChunkCoordinate(-1, -1), LocalX: 15, LocalZ: 15),
            (WorldX: -16, WorldY: 23, WorldZ: -16, Block: BlockId.Stone, Chunk: new ChunkCoordinate(-1, -1), LocalX: 0, LocalZ: 0),
            (WorldX: -17, WorldY: 24, WorldZ: 31, Block: BlockId.Dirt, Chunk: new ChunkCoordinate(-2, 1), LocalX: 15, LocalZ: 15)
        };

        foreach (var sample in samples)
        {
            host.SetBlock(sample.WorldX, sample.WorldY, sample.WorldZ, sample.Block);

            Assert.Equal(sample.Block, host.GetBlock(sample.WorldX, sample.WorldY, sample.WorldZ));
            Assert.True(host.TryGetChunk(sample.Chunk.X, sample.Chunk.Z, out var chunk), $"Expected chunk {sample.Chunk} to exist.");
            Assert.NotNull(chunk);
            Assert.Equal(sample.Block, chunk!.GetBlock(new LocalBlockCoordinate(sample.LocalX, sample.WorldY, sample.LocalZ)));
        }

        Assert.False(host.TryGetBlock(99, 40, 99, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => host.GetBlock(0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => host.SetBlock(0, ChunkConstants.ChunkHeight, 0, BlockId.Stone));
    }

    [Fact]
    public void NeighborAndBlockFaceHelpersResolveAcrossChunkBoundaries()
    {
        var host = new WorldHost();
        host.Initialize(2222);

        host.GetOrCreateChunk(0, 0).GetSubchunk(0).Fill(BlockId.Air);
        host.GetOrCreateChunk(1, 0).GetSubchunk(0).Fill(BlockId.Air);
        host.GetOrCreateChunk(0, 1).GetSubchunk(0).Fill(BlockId.Air);
        host.GetOrCreateChunk(-1, -1).GetSubchunk(0).Fill(BlockId.Air);

        host.SetBlock(15, 10, 15, BlockId.Stone);
        host.SetBlock(16, 10, 15, BlockId.Dirt);
        host.SetBlock(15, 10, 16, BlockId.Grass);
        host.SetBlock(-1, 9, -1, BlockId.Stone);
        host.SetBlock(-1, 10, -1, BlockId.Dirt);

        var eastNeighbor = host.GetNeighbor(15, 10, 15, BlockFace.PositiveX);
        Assert.Equal(new ChunkCoordinate(1, 0), eastNeighbor.ChunkCoordinate);
        Assert.Equal(new LocalBlockCoordinate(0, 10, 15), eastNeighbor.LocalCoordinate);
        Assert.Equal(BlockId.Dirt, eastNeighbor.BlockId);

        var southNeighbor = host.GetNeighbor(15, 10, 15, BlockFace.PositiveZ);
        Assert.Equal(new ChunkCoordinate(0, 1), southNeighbor.ChunkCoordinate);
        Assert.Equal(new LocalBlockCoordinate(15, 10, 0), southNeighbor.LocalCoordinate);
        Assert.Equal(BlockId.Grass, southNeighbor.BlockId);

        var downNeighbor = host.GetNeighbor(-1, 10, -1, BlockFace.NegativeY);
        Assert.Equal(new ChunkCoordinate(-1, -1), downNeighbor.ChunkCoordinate);
        Assert.Equal(new LocalBlockCoordinate(15, 9, 15), downNeighbor.LocalCoordinate);
        Assert.Equal(BlockId.Stone, downNeighbor.BlockId);

        Assert.False(host.IsBlockFaceVisible(15, 10, 15, BlockFace.PositiveX));
        Assert.True(host.IsBlockFaceVisible(15, 10, 15, BlockFace.NegativeX));
        Assert.False(host.TryGetNeighbor(0, 0, 0, BlockFace.NegativeY, out _));
    }

    [Fact]
    public void DirtyAndMeshStatePropagateAtSubchunkBoundaries()
    {
        var host = new WorldHost();
        host.Initialize(3333);

        var chunk = host.GetOrCreateChunk(0, 0);
        var current = chunk.GetSubchunk(1);
        var below = chunk.GetSubchunk(0);
        var leftChunk = host.GetOrCreateChunk(-1, 0);
        var left = leftChunk.GetSubchunk(1);

        current.MarkMeshBuilt();
        below.MarkMeshBuilt();
        left.MarkMeshBuilt();

        host.SetBlock(0, 16, 0, BlockId.Stone);

        Assert.True(current.IsDirty, "Current subchunk should become dirty after edit.");
        Assert.Equal(SubchunkMeshState.NeedsBuild, current.MeshState);
        Assert.True(below.IsDirty, "Adjacent Y-neighbor subchunk should become dirty at subchunk seam.");
        Assert.Equal(SubchunkMeshState.NeedsBuild, below.MeshState);
        Assert.True(left.IsDirty, "Adjacent X-neighbor chunk subchunk should become dirty at chunk seam.");
        Assert.Equal(SubchunkMeshState.NeedsBuild, left.MeshState);

        current.MarkMeshQueued();
        Assert.Equal(SubchunkMeshState.Queued, current.MeshState);
        current.MarkMeshBuilt();
        Assert.False(current.IsDirty);
        Assert.Equal(SubchunkMeshState.Built, current.MeshState);
    }

    [Fact]
    public void DirtySubchunksCanBeDiscoveredAndClaimedForRebuildInStableOrder()
    {
        var host = new WorldHost();
        host.Initialize(4443);

        var originChunk = host.GetOrCreateChunk(0, 0);
        var eastChunk = host.GetOrCreateChunk(1, 0);
        var southChunk = host.GetOrCreateChunk(0, 1);

        foreach (var chunk in new[] { originChunk, eastChunk, southChunk })
        {
            foreach (var subchunk in chunk.Subchunks)
            {
                subchunk.MarkMeshBuilt();
            }
        }

        host.SetBlock(15, 16, 15, BlockId.Stone);

        var dirty = host.EnumerateDirtySubchunks()
            .Select(entry => (entry.Chunk.Coordinate, entry.SubchunkIndex))
            .ToList();

        Assert.Equal(4, dirty.Count);
        Assert.Equal((new ChunkCoordinate(0, 0), 0), dirty[0]);
        Assert.Equal((new ChunkCoordinate(0, 0), 1), dirty[1]);
        Assert.Equal((new ChunkCoordinate(0, 1), 1), dirty[2]);
        Assert.Equal((new ChunkCoordinate(1, 0), 1), dirty[3]);

        Assert.True(host.TryClaimNextDirtySubchunk(out var firstChunk, out var firstSubchunkIndex));
        Assert.NotNull(firstChunk);
        Assert.Equal(new ChunkCoordinate(0, 0), firstChunk!.Coordinate);
        Assert.Equal(0, firstSubchunkIndex);
        Assert.Equal(SubchunkMeshState.Queued, firstChunk.GetSubchunk(firstSubchunkIndex).MeshState);

        Assert.True(host.TryClaimNextDirtySubchunk(out var secondChunk, out var secondSubchunkIndex));
        Assert.NotNull(secondChunk);
        Assert.Equal(new ChunkCoordinate(0, 0), secondChunk!.Coordinate);
        Assert.Equal(1, secondSubchunkIndex);

        Assert.True(host.TryBuildSubchunkMeshingSnapshot(firstChunk.Coordinate.X, firstChunk.Coordinate.Z, firstSubchunkIndex, out var firstSnapshot));
        Assert.NotNull(firstSnapshot);
        Assert.True(host.TryMarkSubchunkMeshBuilt(firstChunk.Coordinate.X, firstChunk.Coordinate.Z, firstSubchunkIndex, firstSnapshot!.Revision, firstSnapshot.BuildVersion));
        Assert.False(firstChunk.GetSubchunk(firstSubchunkIndex).IsDirty);
        Assert.Equal(SubchunkMeshState.Built, firstChunk.GetSubchunk(firstSubchunkIndex).MeshState);

        Assert.True(host.TryClaimNextDirtySubchunk(out var thirdChunk, out var thirdSubchunkIndex));
        Assert.NotNull(thirdChunk);
        Assert.Equal(new ChunkCoordinate(0, 1), thirdChunk!.Coordinate);
        Assert.Equal(1, thirdSubchunkIndex);

        Assert.True(host.TryClaimNextDirtySubchunk(out var fourthChunk, out var fourthSubchunkIndex));
        Assert.NotNull(fourthChunk);
        Assert.Equal(new ChunkCoordinate(1, 0), fourthChunk!.Coordinate);
        Assert.Equal(1, fourthSubchunkIndex);

        Assert.False(host.TryClaimNextDirtySubchunk(out _, out _));
        Assert.False(host.TryMarkSubchunkMeshBuilt(99, 99, 0, 1, 0));
    }

    [Fact]
    public void ClaimNextDirtyCanReturnAReadyMeshingSnapshotInStableOrder()
    {
        var host = new WorldHost();
        host.Initialize(4447);

        var originChunk = host.GetOrCreateChunk(0, 0);
        var eastChunk = host.GetOrCreateChunk(1, 0);
        var southChunk = host.GetOrCreateChunk(0, 1);

        foreach (var chunk in new[] { originChunk, eastChunk, southChunk })
        {
            foreach (var subchunk in chunk.Subchunks)
            {
                subchunk.MarkMeshBuilt();
            }
        }

        host.SetBlock(15, 16, 15, BlockId.Stone);

        var claimedSnapshots = new List<SubchunkMeshingSnapshot>();
        while (host.TryClaimNextDirtySubchunkSnapshot(out var snapshot))
        {
            Assert.NotNull(snapshot);
            claimedSnapshots.Add(snapshot!);
        }

        Assert.Equal(4, claimedSnapshots.Count);
        Assert.Equal(new ChunkCoordinate(0, 0), claimedSnapshots[0].ChunkCoordinate);
        Assert.Equal(0, claimedSnapshots[0].SubchunkIndex);
        Assert.Equal(new ChunkCoordinate(0, 0), claimedSnapshots[1].ChunkCoordinate);
        Assert.Equal(1, claimedSnapshots[1].SubchunkIndex);
        Assert.Equal(new ChunkCoordinate(0, 1), claimedSnapshots[2].ChunkCoordinate);
        Assert.Equal(1, claimedSnapshots[2].SubchunkIndex);
        Assert.Equal(new ChunkCoordinate(1, 0), claimedSnapshots[3].ChunkCoordinate);
        Assert.Equal(1, claimedSnapshots[3].SubchunkIndex);

        foreach (var snapshot in claimedSnapshots)
        {
            Assert.Equal(SubchunkMeshState.Queued, host.GetOrCreateChunk(snapshot.ChunkCoordinate.X, snapshot.ChunkCoordinate.Z).GetSubchunk(snapshot.SubchunkIndex).MeshState);
        }

        Assert.False(host.TryClaimNextDirtySubchunkSnapshot(out _));
    }

    [Fact]
    public void ClaimNextDirtySnapshotResultsStillRejectStaleRevisions()
    {
        var host = new WorldHost();
        host.Initialize(4448);

        var chunk = host.GetOrCreateChunk(0, 0);
        chunk.GetSubchunk(0).Fill(BlockId.Air);
        chunk.GetSubchunk(0).MarkMeshBuilt();

        host.SetBlock(1, 1, 1, BlockId.Stone);

        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var staleSnapshot));
        Assert.NotNull(staleSnapshot);
        Assert.Equal(SubchunkMeshState.Queued, chunk.GetSubchunk(0).MeshState);

        var staleOutput = new SubchunkMeshingOutput(
            staleSnapshot!.ChunkCoordinate,
            staleSnapshot.SubchunkIndex,
            staleSnapshot.Revision,
            staleSnapshot.BuildVersion,
            SubchunkMeshing.BuildVisibleFaces(staleSnapshot));

        host.SetBlock(2, 1, 1, BlockId.Dirt);

        Assert.False(host.TryCompleteSubchunkMeshing(staleOutput));
        Assert.True(chunk.GetSubchunk(0).IsDirty);
        Assert.Equal(SubchunkMeshState.NeedsBuild, chunk.GetSubchunk(0).MeshState);

        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var freshSnapshot));
        Assert.NotNull(freshSnapshot);

        var freshOutput = new SubchunkMeshingOutput(
            freshSnapshot!.ChunkCoordinate,
            freshSnapshot.SubchunkIndex,
            freshSnapshot.Revision,
            freshSnapshot.BuildVersion,
            SubchunkMeshing.BuildVisibleFaces(freshSnapshot));

        Assert.True(host.TryCompleteSubchunkMeshing(freshOutput));
        Assert.False(chunk.GetSubchunk(0).IsDirty);
        Assert.Equal(SubchunkMeshState.Built, chunk.GetSubchunk(0).MeshState);
        Assert.Equal(freshSnapshot.Revision, chunk.GetSubchunk(0).Revision);
        Assert.Equal(freshSnapshot.BuildVersion + 1, chunk.GetSubchunk(0).BuildVersion);
    }

    [Fact]
    public void ClaimedSnapshotMeshingHelperAcceptsCurrentOutput()
    {
        var host = new WorldHost();
        host.Initialize(4449);

        var chunk = host.GetOrCreateChunk(0, 0);
        chunk.GetSubchunk(0).Fill(BlockId.Air);
        chunk.GetSubchunk(0).MarkMeshBuilt();

        host.SetBlock(1, 1, 1, BlockId.Stone);

        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var snapshot));
        Assert.NotNull(snapshot);
        Assert.True(host.TryCreateClaimedSubchunkMeshingOutput(snapshot!, SubchunkMeshing.BuildVisibleFaces(snapshot!), out var output));
        Assert.NotNull(output);
        Assert.True(host.TryCompleteSubchunkMeshing(snapshot!, output!));
        Assert.False(chunk.GetSubchunk(0).IsDirty);
        Assert.Equal(SubchunkMeshState.Built, chunk.GetSubchunk(0).MeshState);
        Assert.Equal(snapshot!.Revision, output!.Revision);
        Assert.Equal(snapshot.BuildVersion, output.BuildVersion);
    }

    [Fact]
    public void BuiltSubchunkMeshingOutputCanBeRetrievedAfterSuccessfulCompletion()
    {
        var host = new WorldHost();
        host.Initialize(4452);

        var chunk = host.GetOrCreateChunk(0, 0);
        chunk.GetSubchunk(0).Fill(BlockId.Air);
        chunk.GetSubchunk(0).MarkMeshBuilt();

        host.SetBlock(1, 1, 1, BlockId.Stone);

        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var snapshot));
        Assert.NotNull(snapshot);

        var output = SubchunkMeshing.BuildOutput(snapshot!);
        Assert.True(host.TryCompleteSubchunkMeshing(snapshot!, output));
        Assert.True(host.TryGetBuiltSubchunkMeshingOutput(0, 0, 0, out var cachedOutput));
        Assert.NotNull(cachedOutput);
        Assert.Equal(output.ChunkCoordinate, cachedOutput!.ChunkCoordinate);
        Assert.Equal(output.SubchunkIndex, cachedOutput.SubchunkIndex);
        Assert.Equal(output.Revision, cachedOutput.Revision);
        Assert.Equal(output.BuildVersion, cachedOutput.BuildVersion);
        Assert.Equal(output.VisibleFaces.Count, cachedOutput.VisibleFaces.Count);
    }

    [Fact]
    public void BuiltMeshingOutputsEnumerateInStableChunkSubchunkOrder()
    {
        var host = new WorldHost();
        host.Initialize(4454);

        var westChunk = host.GetOrCreateChunk(-1, 0);
        var originChunk = host.GetOrCreateChunk(0, 0);

        foreach (var chunk in new[] { westChunk, originChunk })
        {
            chunk.GetSubchunk(0).Fill(BlockId.Air);
            chunk.GetSubchunk(1).Fill(BlockId.Air);

            foreach (var subchunk in chunk.Subchunks)
            {
                subchunk.MarkMeshBuilt();
            }
        }

        host.SetBlock(-15, 1, 1, BlockId.Stone);
        host.SetBlock(1, 17, 1, BlockId.Dirt);

        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var firstSnapshot));
        Assert.True(host.TryCompleteSubchunkMeshing(firstSnapshot!, SubchunkMeshing.BuildOutput(firstSnapshot!)));
        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var secondSnapshot));
        Assert.True(host.TryCompleteSubchunkMeshing(secondSnapshot!, SubchunkMeshing.BuildOutput(secondSnapshot!)));

        var builtOutputs = host.EnumerateBuiltSubchunkMeshingOutputs().ToList();

        Assert.Equal(2, builtOutputs.Count);
        Assert.Equal(new ChunkCoordinate(-1, 0), builtOutputs[0].ChunkCoordinate);
        Assert.Equal(0, builtOutputs[0].SubchunkIndex);
        Assert.Equal(new ChunkCoordinate(0, 0), builtOutputs[1].ChunkCoordinate);
        Assert.Equal(1, builtOutputs[1].SubchunkIndex);
    }

    [Fact]
    public void MeshingSchedulerProcessesDirtySnapshotsWithinBudget()
    {
        var host = new WorldHost();
        host.Initialize(4459);

        var westChunk = host.GetOrCreateChunk(-1, 0);
        var originChunk = host.GetOrCreateChunk(0, 0);
        var scheduler = new WorldMeshingScheduler();

        foreach (var chunk in new[] { westChunk, originChunk })
        {
            chunk.GetSubchunk(0).Fill(BlockId.Air);
            chunk.GetSubchunk(1).Fill(BlockId.Air);

            foreach (var subchunk in chunk.Subchunks)
            {
                subchunk.MarkMeshBuilt();
            }
        }

        host.SetBlock(-15, 1, 1, BlockId.Stone);
        host.SetBlock(1, 17, 1, BlockId.Dirt);

        Assert.Equal(1, scheduler.ProcessDirtySubchunkSnapshots(host, 1));

        var firstPassOutputs = host.EnumerateBuiltSubchunkMeshingOutputs().ToList();
        Assert.Single(firstPassOutputs);
        Assert.Equal(new ChunkCoordinate(-1, 0), firstPassOutputs[0].ChunkCoordinate);
        Assert.Equal(0, firstPassOutputs[0].SubchunkIndex);

        Assert.Equal(1, scheduler.ProcessDirtySubchunkSnapshots(host, 1));

        var secondPassOutputs = host.EnumerateBuiltSubchunkMeshingOutputs().ToList();
        Assert.Equal(2, secondPassOutputs.Count);
        Assert.Equal(new ChunkCoordinate(-1, 0), secondPassOutputs[0].ChunkCoordinate);
        Assert.Equal(0, secondPassOutputs[0].SubchunkIndex);
        Assert.Equal(new ChunkCoordinate(0, 0), secondPassOutputs[1].ChunkCoordinate);
        Assert.Equal(1, secondPassOutputs[1].SubchunkIndex);
    }

    [Fact]
    public void BuiltMeshingOutputsCanBeDrainedOnceInStableChunkSubchunkOrder()
    {
        var host = new WorldHost();
        host.Initialize(4456);

        var westChunk = host.GetOrCreateChunk(-1, 0);
        var originChunk = host.GetOrCreateChunk(0, 0);
        var scheduler = new WorldMeshingScheduler();

        foreach (var chunk in new[] { westChunk, originChunk })
        {
            chunk.GetSubchunk(0).Fill(BlockId.Air);
            chunk.GetSubchunk(1).Fill(BlockId.Air);

            foreach (var subchunk in chunk.Subchunks)
            {
                subchunk.MarkMeshBuilt();
            }
        }

        host.SetBlock(-15, 1, 1, BlockId.Stone);
        host.SetBlock(1, 17, 1, BlockId.Dirt);

        Assert.Equal(2, scheduler.ProcessDirtySubchunkSnapshots(host));

        Assert.True(host.TryDrainNextBuiltSubchunkMeshingOutput(out var firstOutput));
        Assert.NotNull(firstOutput);
        Assert.Equal(new ChunkCoordinate(-1, 0), firstOutput!.ChunkCoordinate);
        Assert.Equal(0, firstOutput.SubchunkIndex);

        Assert.True(host.TryDrainNextBuiltSubchunkMeshingOutput(out var secondOutput));
        Assert.NotNull(secondOutput);
        Assert.Equal(new ChunkCoordinate(0, 0), secondOutput!.ChunkCoordinate);
        Assert.Equal(1, secondOutput.SubchunkIndex);

        Assert.False(host.TryDrainNextBuiltSubchunkMeshingOutput(out _));
        Assert.Equal(2, host.EnumerateBuiltSubchunkMeshingOutputs().Count());
    }

    [Fact]
    public void RendererPickupLoopQueuesBuiltOutputsInStableOrderAndProcessesUploadsWithinBudget()
    {
        var host = new WorldHost();
        host.Initialize(4458);

        var westChunk = host.GetOrCreateChunk(-1, 0);
        var originChunk = host.GetOrCreateChunk(0, 0);
        var scheduler = new WorldMeshingScheduler();

        foreach (var chunk in new[] { westChunk, originChunk })
        {
            chunk.GetSubchunk(0).Fill(BlockId.Air);
            chunk.GetSubchunk(1).Fill(BlockId.Air);

            foreach (var subchunk in chunk.Subchunks)
            {
                subchunk.MarkMeshBuilt();
            }
        }

        host.SetBlock(-15, 1, 1, BlockId.Stone);
        host.SetBlock(1, 17, 1, BlockId.Dirt);

        Assert.Equal(2, scheduler.ProcessDirtySubchunkSnapshots(host));

        var renderer = new RendererHost();
        renderer.Initialize();

        Assert.Equal(2, renderer.PickupBuiltSubchunkMeshingOutputs(host));
        Assert.Equal(2, renderer.PendingSubchunkUploadCount);
        Assert.Equal(0, renderer.UploadedSubchunkMeshCount);
        Assert.True(renderer.TryGetSubchunkUploadState(new ChunkCoordinate(-1, 0), 0, out var firstState));
        Assert.Equal(RendererSubchunkUploadState.PendingUpload, firstState);
        Assert.True(renderer.TryGetSubchunkUploadState(new ChunkCoordinate(0, 0), 1, out var secondState));
        Assert.Equal(RendererSubchunkUploadState.PendingUpload, secondState);
        Assert.False(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(-1, 0), 0, out _));
        Assert.False(host.TryDrainNextBuiltSubchunkMeshingOutput(out _));

        Assert.Equal(1, renderer.ProcessPendingUploads(1));
        Assert.Equal(1, renderer.PendingSubchunkUploadCount);
        Assert.Equal(1, renderer.UploadedSubchunkMeshCount);
        Assert.True(renderer.TryGetSubchunkUploadState(new ChunkCoordinate(-1, 0), 0, out firstState));
        Assert.Equal(RendererSubchunkUploadState.Uploaded, firstState);
        Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(-1, 0), 0, out var firstUploadedOutput));
        Assert.NotNull(firstUploadedOutput);
        Assert.True(renderer.TryGetSubchunkUploadState(new ChunkCoordinate(0, 0), 1, out secondState));
        Assert.Equal(RendererSubchunkUploadState.PendingUpload, secondState);
        Assert.False(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(0, 0), 1, out _));

        Assert.Equal(1, renderer.ProcessPendingUploads(1));
        Assert.Equal(0, renderer.PendingSubchunkUploadCount);
        Assert.Equal(2, renderer.UploadedSubchunkMeshCount);
        Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(0, 0), 1, out var secondUploadedOutput));
        Assert.NotNull(secondUploadedOutput);
        Assert.Equal(0, renderer.ProcessPendingUploads(1));
        Assert.Equal(0, renderer.PickupBuiltSubchunkMeshingOutputs(host));

        renderer.Shutdown();
        Assert.Equal(0, renderer.PendingSubchunkUploadCount);
        Assert.Equal(0, renderer.UploadedSubchunkMeshCount);
    }

    [Fact]
    public void RendererReplacementOverwritesOlderPendingAndUploadedSubchunkState()
    {
        var host = new WorldHost();
        host.Initialize(4460);

        var chunk = host.GetOrCreateChunk(0, 0);
        chunk.GetSubchunk(0).Fill(BlockId.Air);
        chunk.GetSubchunk(0).MarkMeshBuilt();

        var renderer = new RendererHost();
        renderer.Initialize();

        host.SetBlock(1, 1, 1, BlockId.Stone);
        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var firstSnapshot));
        Assert.True(host.TryCompleteSubchunkMeshing(firstSnapshot!, SubchunkMeshing.BuildOutput(firstSnapshot!)));
        Assert.Equal(1, renderer.PickupBuiltSubchunkMeshingOutputs(host));

        host.SetBlock(2, 1, 1, BlockId.Dirt);
        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var secondSnapshot));
        Assert.True(host.TryCompleteSubchunkMeshing(secondSnapshot!, SubchunkMeshing.BuildOutput(secondSnapshot!)));
        Assert.Equal(1, renderer.PickupBuiltSubchunkMeshingOutputs(host));
        Assert.Equal(1, renderer.PendingSubchunkUploadCount);
        Assert.Equal(0, renderer.UploadedSubchunkMeshCount);
        Assert.Equal(1, renderer.ProcessPendingUploads(1));
        Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(0, 0), 0, out var secondUploadedOutput));
        Assert.NotNull(secondUploadedOutput);
        Assert.Equal(secondSnapshot!.BuildVersion, secondUploadedOutput!.BuildVersion);

        host.SetBlock(3, 1, 1, BlockId.Grass);
        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var thirdSnapshot));
        Assert.True(host.TryCompleteSubchunkMeshing(thirdSnapshot!, SubchunkMeshing.BuildOutput(thirdSnapshot!)));
        Assert.Equal(1, renderer.PickupBuiltSubchunkMeshingOutputs(host));
        Assert.Equal(1, renderer.PendingSubchunkUploadCount);
        Assert.Equal(0, renderer.UploadedSubchunkMeshCount);
        Assert.False(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(0, 0), 0, out _));
        Assert.Equal(1, renderer.ProcessPendingUploads(1));
        Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(0, 0), 0, out var thirdUploadedOutput));
        Assert.NotNull(thirdUploadedOutput);
        Assert.Equal(thirdSnapshot!.BuildVersion, thirdUploadedOutput!.BuildVersion);
    }

    [Fact]
    public void GameHostCanLoadARestoredWorldAndResumeMeshingFlow()
    {
        var persistence = new PersistenceHost();
        persistence.Initialize();

        try
        {
            var source = new WorldHost();
            source.Initialize(4461);

            var originChunk = source.GetOrCreateChunk(0, 0);
            var eastChunk = source.GetOrCreateChunk(1, 0);

            foreach (var chunk in new[] { originChunk, eastChunk })
            {
                chunk.GetSubchunk(0).Fill(BlockId.Air);
                chunk.GetSubchunk(1).Fill(BlockId.Air);

                foreach (var subchunk in chunk.Subchunks)
                {
                    subchunk.MarkMeshBuilt();
                }
            }

            source.SetBlock(15, 16, 1, BlockId.Stone);

            var restored = persistence.LoadWorldFromJson(persistence.SaveWorldToJson(source));
            var game = new GameHost();
            game.Initialize();
            game.LoadWorld(restored);

            Assert.Equal(4461, game.World.Seed);
            Assert.Equal(BlockId.Stone, game.World.GetBlock(15, 16, 1));

            game.Update();

            var renderer = new RendererHost();
            renderer.Initialize();

            Assert.Equal(16, renderer.PickupBuiltSubchunkMeshingOutputs(game.World));
            Assert.Equal(16, renderer.PendingSubchunkUploadCount);
            Assert.Equal(16, renderer.ProcessPendingUploads(16));
            Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(0, 0), 1, out _));
            Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(1, 0), 1, out _));
            Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(0, 0), 0, out _));
            Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(1, 0), 0, out _));

            renderer.Shutdown();
            game.Shutdown();
        }
        finally
        {
            persistence.Shutdown();
        }
    }

    [Fact]
    public void AppCreateWorldHandlerSwapsRuntimeWorldAndHandlesInvalidSeedInput()
    {
        var game = new GameHost();
        game.Initialize(1111);
        var originalWorld = game.World;

        var invalidMessage = AppHost.HandleCreateWorld(game, "abc");
        Assert.Contains("Invalid seed", invalidMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Same(originalWorld, game.World);
        Assert.Equal(1111, game.World.Seed);

        var createdMessage = AppHost.HandleCreateWorld(game, "2222");
        Assert.Contains("2222", createdMessage, StringComparison.OrdinalIgnoreCase);
        Assert.NotSame(originalWorld, game.World);
        Assert.Equal(2222, game.World.Seed);

        game.World.GetOrCreateChunk(0, 0).GetSubchunk(0).Fill(BlockId.Air);
        game.World.SetBlock(1, 1, 1, BlockId.Stone);
        game.Update();
        Assert.True(game.World.TryDrainNextBuiltSubchunkMeshingOutput(out _));

        game.Shutdown();
    }

    [Fact]
    public void AppSaveLoadHandlersRoundtripThroughGameAndResumeRendererFlow()
    {
        var persistence = new PersistenceHost();
        persistence.Initialize();

        var savePath = persistence.GetDefaultSavePath();
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
        }

        try
        {
            var game = new GameHost();
            game.Initialize(4462);

            var originChunk = game.World.GetOrCreateChunk(0, 0);
            var eastChunk = game.World.GetOrCreateChunk(1, 0);

            foreach (var chunk in new[] { originChunk, eastChunk })
            {
                chunk.GetSubchunk(0).Fill(BlockId.Air);
                chunk.GetSubchunk(1).Fill(BlockId.Air);

                foreach (var subchunk in chunk.Subchunks)
                {
                    subchunk.MarkMeshBuilt();
                }
            }

            game.World.SetBlock(15, 16, 1, BlockId.Stone);

            var missingLoadMessage = AppHost.HandleLoadWorld(game, persistence);
            Assert.Contains("No save file", missingLoadMessage, StringComparison.OrdinalIgnoreCase);

            var saveMessage = AppHost.HandleSaveWorld(game, persistence);
            Assert.Contains(savePath, saveMessage, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(savePath));

            game.CreateWorld(99);
            Assert.Equal(99, game.World.Seed);

            var loadMessage = AppHost.HandleLoadWorld(game, persistence);
            Assert.Contains("Loaded world seed 4462", loadMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(4462, game.World.Seed);
            Assert.Equal(BlockId.Stone, game.World.GetBlock(15, 16, 1));

            game.Update();

            var renderer = new RendererHost();
            renderer.Initialize();

            Assert.Equal(16, renderer.PickupBuiltSubchunkMeshingOutputs(game.World));
            Assert.Equal(16, renderer.ProcessPendingUploads(16));
            Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(0, 0), 0, out _));
            Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(0, 0), 1, out _));
            Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(1, 0), 0, out _));
            Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(1, 0), 1, out _));

            renderer.Shutdown();
            game.Shutdown();
        }
        finally
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            persistence.Shutdown();
        }
    }

    [Fact]
    public void GameUpdateBuildsMeshesAndRendererUploadPumpConsumesThem()
    {
        var game = new GameHost();
        game.Initialize();

        var westChunk = game.World.GetOrCreateChunk(-1, 0);
        var originChunk = game.World.GetOrCreateChunk(0, 0);

        foreach (var chunk in new[] { westChunk, originChunk })
        {
            chunk.GetSubchunk(0).Fill(BlockId.Air);
            chunk.GetSubchunk(1).Fill(BlockId.Air);

            foreach (var subchunk in chunk.Subchunks)
            {
                subchunk.MarkMeshBuilt();
            }
        }

        game.World.SetBlock(-15, 1, 1, BlockId.Stone);
        game.World.SetBlock(1, 17, 1, BlockId.Dirt);

        game.Update();

        var renderer = new RendererHost();
        renderer.Initialize();

        Assert.Equal(2, renderer.PickupBuiltSubchunkMeshingOutputs(game.World));
        Assert.Equal(2, renderer.PendingSubchunkUploadCount);
        Assert.Equal(2, renderer.ProcessPendingUploads(2));
        Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(-1, 0), 0, out _));
        Assert.True(renderer.TryGetUploadedSubchunkMesh(new ChunkCoordinate(0, 0), 1, out _));
        Assert.False(game.World.TryDrainNextBuiltSubchunkMeshingOutput(out _));

        renderer.Shutdown();
        game.Shutdown();
    }

    [Fact]
    public void BuiltMeshingOutputDrainDoesNotReturnTheSameOutputTwiceUnlessRebuilt()
    {
        var host = new WorldHost();
        host.Initialize(4457);

        var chunk = host.GetOrCreateChunk(0, 0);
        chunk.GetSubchunk(0).Fill(BlockId.Air);
        chunk.GetSubchunk(0).MarkMeshBuilt();

        host.SetBlock(1, 1, 1, BlockId.Stone);

        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var firstSnapshot));
        Assert.True(host.TryCompleteSubchunkMeshing(firstSnapshot!, SubchunkMeshing.BuildOutput(firstSnapshot!)));
        Assert.True(host.TryDrainNextBuiltSubchunkMeshingOutput(out var firstDrain));
        Assert.NotNull(firstDrain);
        Assert.False(host.TryDrainNextBuiltSubchunkMeshingOutput(out _));
        Assert.True(host.TryGetBuiltSubchunkMeshingOutput(0, 0, 0, out var cachedOutput));
        Assert.NotNull(cachedOutput);
        Assert.Equal(firstDrain!.BuildVersion, cachedOutput!.BuildVersion);

        host.SetBlock(2, 1, 1, BlockId.Dirt);

        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var rebuiltSnapshot));
        Assert.True(host.TryCompleteSubchunkMeshing(rebuiltSnapshot!, SubchunkMeshing.BuildOutput(rebuiltSnapshot!)));
        Assert.True(host.TryDrainNextBuiltSubchunkMeshingOutput(out var rebuiltDrain));
        Assert.NotNull(rebuiltDrain);
        Assert.Equal(rebuiltSnapshot!.BuildVersion, rebuiltDrain!.BuildVersion);
        Assert.False(host.TryDrainNextBuiltSubchunkMeshingOutput(out _));
    }

    [Fact]
    public void BuiltMeshingOutputEnumerationExcludesDirtyOrUncachedSubchunks()
    {
        var host = new WorldHost();
        host.Initialize(4455);

        var chunk = host.GetOrCreateChunk(0, 0);
        chunk.GetSubchunk(0).Fill(BlockId.Air);
        chunk.GetSubchunk(1).Fill(BlockId.Air);

        foreach (var subchunk in chunk.Subchunks)
        {
            subchunk.MarkMeshBuilt();
        }

        host.SetBlock(1, 1, 1, BlockId.Stone);
        host.SetBlock(1, 17, 1, BlockId.Dirt);

        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var firstSnapshot));
        Assert.True(host.TryCompleteSubchunkMeshing(firstSnapshot!, SubchunkMeshing.BuildOutput(firstSnapshot!)));

        var builtOutputs = host.EnumerateBuiltSubchunkMeshingOutputs().ToList();
        Assert.Single(builtOutputs);
        Assert.Equal(0, builtOutputs[0].SubchunkIndex);

        host.SetBlock(2, 1, 1, BlockId.Grass);

        Assert.Empty(host.EnumerateBuiltSubchunkMeshingOutputs());
    }

    [Fact]
    public void BuiltSubchunkMeshingOutputIsInvalidatedByLaterDirtyEdits()
    {
        var host = new WorldHost();
        host.Initialize(4453);

        var chunk = host.GetOrCreateChunk(0, 0);
        chunk.GetSubchunk(0).Fill(BlockId.Air);
        chunk.GetSubchunk(0).MarkMeshBuilt();

        host.SetBlock(1, 1, 1, BlockId.Stone);

        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var snapshot));
        Assert.NotNull(snapshot);

        var output = SubchunkMeshing.BuildOutput(snapshot!);
        Assert.True(host.TryCompleteSubchunkMeshing(snapshot!, output));
        Assert.True(host.TryGetBuiltSubchunkMeshingOutput(0, 0, 0, out _));

        host.SetBlock(2, 1, 1, BlockId.Dirt);

        Assert.False(host.TryGetBuiltSubchunkMeshingOutput(0, 0, 0, out _));
        Assert.Null(chunk.GetSubchunk(0).CurrentMeshingOutput);
    }

    [Fact]
    public void ClaimedSnapshotMeshingHelperRejectsStaleOutput()
    {
        var host = new WorldHost();
        host.Initialize(4450);

        var chunk = host.GetOrCreateChunk(0, 0);
        chunk.GetSubchunk(0).Fill(BlockId.Air);
        chunk.GetSubchunk(0).MarkMeshBuilt();

        host.SetBlock(1, 1, 1, BlockId.Stone);

        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var snapshot));
        Assert.NotNull(snapshot);

        var output = SubchunkMeshing.BuildOutput(snapshot!);
        host.SetBlock(2, 1, 1, BlockId.Dirt);

        Assert.False(host.TryCreateClaimedSubchunkMeshingOutput(snapshot!, output.VisibleFaces, out _));
        Assert.False(host.TryCompleteSubchunkMeshing(snapshot!, output));
        Assert.True(chunk.GetSubchunk(0).IsDirty);
        Assert.Equal(SubchunkMeshState.NeedsBuild, chunk.GetSubchunk(0).MeshState);
    }

    [Fact]
    public void ClaimedSnapshotMeshingHelperRejectsWrongTargetOutput()
    {
        var host = new WorldHost();
        host.Initialize(4451);

        var chunk = host.GetOrCreateChunk(0, 0);
        chunk.GetSubchunk(0).Fill(BlockId.Air);
        chunk.GetSubchunk(0).MarkMeshBuilt();

        host.SetBlock(1, 1, 1, BlockId.Stone);

        Assert.True(host.TryClaimNextDirtySubchunkSnapshot(out var snapshot));
        Assert.NotNull(snapshot);

        var validOutput = SubchunkMeshing.BuildOutput(snapshot!);
        var wrongTargetOutput = validOutput with { SubchunkIndex = 1 };

        Assert.False(host.TryCompleteSubchunkMeshing(snapshot!, wrongTargetOutput));
        Assert.True(chunk.GetSubchunk(0).IsDirty);
        Assert.Equal(SubchunkMeshState.Queued, chunk.GetSubchunk(0).MeshState);
        Assert.True(host.TryCompleteSubchunkMeshing(snapshot!, validOutput));
    }

    [Fact]
    public void MeshingSnapshotCapturesSeamDataAcrossChunkAndSubchunkBoundaries()
    {
        var host = new WorldHost();
        host.Initialize(4445);

        var originChunk = host.GetOrCreateChunk(0, 0);
        var eastChunk = host.GetOrCreateChunk(1, 0);

        originChunk.GetSubchunk(0).Fill(BlockId.Air);
        originChunk.GetSubchunk(1).Fill(BlockId.Air);
        eastChunk.GetSubchunk(0).Fill(BlockId.Air);

        foreach (var subchunk in new[] { originChunk.GetSubchunk(0), originChunk.GetSubchunk(1), eastChunk.GetSubchunk(0) })
        {
            subchunk.MarkMeshBuilt();
        }

        host.SetBlock(15, 15, 0, BlockId.Stone);
        host.SetBlock(16, 15, 0, BlockId.Dirt);
        host.SetBlock(15, 16, 0, BlockId.Grass);

        Assert.True(host.TryBuildSubchunkMeshingSnapshot(0, 0, 0, out var snapshot));
        Assert.NotNull(snapshot);

        var visibleFaces = SubchunkMeshing.BuildVisibleFaces(snapshot!);
        var targetFaces = visibleFaces.Where(face => face.WorldX == 15 && face.WorldY == 15 && face.WorldZ == 0).ToList();
        var worldTargetFaces = SubchunkMeshing.EnumerateVisibleFaces(host, originChunk, 0)
            .Where(face => face.WorldX == 15 && face.WorldY == 15 && face.WorldZ == 0)
            .ToList();

        Assert.Equal(worldTargetFaces.Count, targetFaces.Count);
        Assert.Equal(
            string.Join(',', worldTargetFaces.Select(face => face.Face).OrderBy(face => face)),
            string.Join(',', targetFaces.Select(face => face.Face).OrderBy(face => face)));
        Assert.DoesNotContain(targetFaces, face => face.Face == BlockFace.PositiveX);
        Assert.DoesNotContain(targetFaces, face => face.Face == BlockFace.PositiveY);
        Assert.Equal(SubchunkMeshing.CountVisibleFaces(host, originChunk, 0), SubchunkMeshing.CountVisibleFaces(snapshot!));
    }

    [Fact]
    public void MeshingSnapshotResultsRejectStaleRevisionsAndAcceptCurrentOnes()
    {
        var host = new WorldHost();
        host.Initialize(4446);

        var chunk = host.GetOrCreateChunk(0, 0);
        chunk.GetSubchunk(0).Fill(BlockId.Air);
        chunk.GetSubchunk(0).MarkMeshBuilt();

        host.SetBlock(1, 1, 1, BlockId.Stone);

        Assert.True(host.TryClaimNextDirtySubchunk(out var claimedChunk, out var subchunkIndex));
        Assert.NotNull(claimedChunk);
        Assert.Equal(0, subchunkIndex);
        Assert.True(host.TryBuildSubchunkMeshingSnapshot(0, 0, 0, out var staleSnapshot));
        Assert.NotNull(staleSnapshot);

        var staleOutput = new SubchunkMeshingOutput(
            staleSnapshot!.ChunkCoordinate,
            staleSnapshot.SubchunkIndex,
            staleSnapshot.Revision,
            staleSnapshot.BuildVersion,
            SubchunkMeshing.BuildVisibleFaces(staleSnapshot));

        host.SetBlock(2, 1, 1, BlockId.Dirt);

        Assert.False(host.TryCompleteSubchunkMeshing(staleOutput));
        Assert.True(chunk.GetSubchunk(0).IsDirty);
        Assert.Equal(SubchunkMeshState.NeedsBuild, chunk.GetSubchunk(0).MeshState);

        Assert.True(host.TryBuildSubchunkMeshingSnapshot(0, 0, 0, out var freshSnapshot));
        Assert.NotNull(freshSnapshot);

        var freshOutput = new SubchunkMeshingOutput(
            freshSnapshot!.ChunkCoordinate,
            freshSnapshot.SubchunkIndex,
            freshSnapshot.Revision,
            freshSnapshot.BuildVersion,
            SubchunkMeshing.BuildVisibleFaces(freshSnapshot));

        Assert.True(host.TryCompleteSubchunkMeshing(freshOutput));
        Assert.False(chunk.GetSubchunk(0).IsDirty);
        Assert.Equal(SubchunkMeshState.Built, chunk.GetSubchunk(0).MeshState);
        Assert.Equal(freshSnapshot.Revision, chunk.GetSubchunk(0).Revision);
        Assert.Equal(freshSnapshot.BuildVersion + 1, chunk.GetSubchunk(0).BuildVersion);
    }

    [Fact]
    public void MeshingBaseCountsVisibleFacesAcrossChunkBoundaries()
    {
        var host = new WorldHost();
        host.Initialize(4444);

        var chunk = host.GetOrCreateChunk(0, 0);
        var target = chunk.GetSubchunk(0);
        target.Fill(BlockId.Air);
        target.MarkMeshBuilt();

        host.SetBlock(15, 1, 1, BlockId.Stone);
        host.SetBlock(16, 1, 1, BlockId.Dirt);

        var visibleFaces = SubchunkMeshing.EnumerateVisibleFaces(host, chunk, 0).ToList();
        var targetFaces = visibleFaces.Where(face => face.WorldX == 15 && face.WorldY == 1 && face.WorldZ == 1).ToList();

        Assert.Equal(5, targetFaces.Count);
        Assert.DoesNotContain(targetFaces, face => face.Face == BlockFace.PositiveX);
        Assert.Equal(5, SubchunkMeshing.CountVisibleFaces(host, chunk, 0));
    }

    [Fact]
    public void ChunkConstantsAndSubchunkLayoutStayConsistent()
    {
        Assert.Equal(16, ChunkConstants.ChunkSizeX);
        Assert.Equal(16, ChunkConstants.ChunkSizeZ);
        Assert.Equal(128, ChunkConstants.ChunkHeight);
        Assert.Equal(16, ChunkConstants.SubchunkSize);
        Assert.Equal(8, ChunkConstants.SubchunkCountPerChunk);

        var chunk = new Chunk(new ChunkCoordinate(0, 0));
        Assert.Equal(ChunkConstants.SubchunkCountPerChunk, chunk.Subchunks.Count);

        foreach (var subchunk in chunk.Subchunks)
        {
            Assert.Equal(ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize, subchunk.AsSpan().Length);
        }
    }

    private static void AssertChunksEqual(Chunk expected, Chunk actual)
    {
        Assert.Equal(expected.Coordinate, actual.Coordinate);
        Assert.Equal(expected.State, actual.State);
        Assert.Equal(expected.Subchunks.Count, actual.Subchunks.Count);

        for (var i = 0; i < expected.Subchunks.Count; i++)
        {
            var expectedBlocks = expected.Subchunks[i].AsSpan();
            var actualBlocks = actual.Subchunks[i].AsSpan();
            Assert.Equal(expectedBlocks.Length, actualBlocks.Length);

            for (var blockIndex = 0; blockIndex < expectedBlocks.Length; blockIndex++)
            {
                Assert.Equal(expectedBlocks[blockIndex], actualBlocks[blockIndex]);
            }
        }
    }

    private static SubchunkSaveSnapshot[] CreateValidSubchunkSnapshots()
    {
        return Enumerable.Range(0, ChunkConstants.SubchunkCountPerChunk)
            .Select(_ => new SubchunkSaveSnapshot(new BlockId[ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize]))
            .ToArray();
    }

    private static void AssertLocal(LocalBlockCoordinate coordinate, int x, int y, int z)
    {
        Assert.Equal(x, coordinate.X);
        Assert.Equal(y, coordinate.Y);
        Assert.Equal(z, coordinate.Z);
    }
}

