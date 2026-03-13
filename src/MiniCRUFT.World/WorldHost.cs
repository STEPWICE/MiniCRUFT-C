namespace MiniCRUFT.World;

public sealed record WorldSaveSnapshot(int FormatVersion, int Seed, IReadOnlyList<ChunkSaveSnapshot> Chunks);
public sealed record ChunkSaveSnapshot(int X, int Z, ChunkState State, IReadOnlyList<SubchunkSaveSnapshot> Subchunks);
public sealed record SubchunkSaveSnapshot(BlockId[] Blocks);

public sealed class WorldHost
{

    private readonly Dictionary<ChunkCoordinate, Chunk> _chunks = new();
    private readonly HashSet<ChunkCoordinate> _persistedChunkCoordinates = new();
    private WorldGenerator? _generator;

    public int Seed { get; private set; } = 12345;
    public int LoadedChunkCount => _chunks.Count;
    public int GeneratedChunkCount { get; private set; }

    public IReadOnlyCollection<Chunk> Chunks => _chunks.Values;

    public WorldSaveSnapshot ExportSnapshot()
    {
        var chunks = _persistedChunkCoordinates
            .OrderBy(coordinate => coordinate.X)
            .ThenBy(coordinate => coordinate.Z)
            .Select(coordinate => ExportChunkSnapshot(_chunks[coordinate]))
            .ToArray();

        return new WorldSaveSnapshot(0, Seed, chunks);
    }

    public void ImportSnapshot(WorldSaveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var importedChunks = ValidateAndImportChunks(snapshot);

        Initialize(snapshot.Seed);

        foreach (var chunk in importedChunks)
        {
            _chunks.Add(chunk.Coordinate, chunk);
            _persistedChunkCoordinates.Add(chunk.Coordinate);
        }

        GeneratedChunkCount = _chunks.Count;
    }

    public BlockId GetBlock(int worldX, int worldY, int worldZ)
    {
        var (chunk, localCoordinate) = ResolveWorldBlock(worldX, worldY, worldZ);
        return chunk.GetBlock(localCoordinate);
    }

    public bool TryGetBlock(int worldX, int worldY, int worldZ, out BlockId blockId)
    {
        ValidateWorldY(worldY);

        var chunkCoordinate = ChunkCoordinate.FromWorldBlock(worldX, worldZ);
        var localCoordinate = LocalBlockCoordinate.FromWorldBlock(worldX, worldY, worldZ);
        if (_chunks.TryGetValue(chunkCoordinate, out var chunk))
        {
            blockId = chunk.GetBlock(localCoordinate);
            return true;
        }

        blockId = BlockId.Air;
        return false;
    }

    public void SetBlock(int worldX, int worldY, int worldZ, BlockId blockId)
    {
        var (chunk, localCoordinate) = ResolveWorldBlock(worldX, worldY, worldZ);
        chunk.SetBlock(localCoordinate, blockId);
        TrackPersistedChunksForEdit(chunk, localCoordinate);
        MarkNeighborSubchunksDirty(chunk, localCoordinate);
    }

    public BlockNeighbor GetNeighbor(int worldX, int worldY, int worldZ, BlockFace face)
    {
        if (!TryGetNeighbor(worldX, worldY, worldZ, face, out var neighbor))
        {
            throw new ArgumentOutOfRangeException(nameof(worldY));
        }

        return neighbor;
    }

    public bool TryGetNeighbor(int worldX, int worldY, int worldZ, BlockFace face, out BlockNeighbor neighbor)
    {
        var (offsetX, offsetY, offsetZ) = face.GetOffset();
        var neighborWorldX = worldX + offsetX;
        var neighborWorldY = worldY + offsetY;
        var neighborWorldZ = worldZ + offsetZ;

        if ((uint)neighborWorldY >= ChunkConstants.ChunkHeight)
        {
            neighbor = default;
            return false;
        }

        var (chunk, localCoordinate, chunkCoordinate) = ResolveWorldBlockWithCoordinate(neighborWorldX, neighborWorldY, neighborWorldZ);
        neighbor = new BlockNeighbor(
            neighborWorldX,
            neighborWorldY,
            neighborWorldZ,
            chunkCoordinate,
            localCoordinate,
            chunk.GetBlock(localCoordinate));

        return true;
    }

    public bool IsBlockFaceVisible(int worldX, int worldY, int worldZ, BlockFace face)
    {
        if (!TryGetNeighbor(worldX, worldY, worldZ, face, out var neighbor))
        {
            return true;
        }

        return !IsOccluding(neighbor.BlockId);
    }

    public void Initialize(int? seed = null)
    {
        Seed = seed ?? Seed;
        _chunks.Clear();
        _persistedChunkCoordinates.Clear();
        GeneratedChunkCount = 0;
        _generator = new WorldGenerator(Seed);
    }

    public Chunk GetOrCreateChunk(int x, int z)
    {
        EnsureInitialized();

        var coordinate = new ChunkCoordinate(x, z);
        if (_chunks.TryGetValue(coordinate, out var existingChunk))
        {
            return existingChunk;
        }

        var generatedChunk = _generator!.GenerateChunk(coordinate);
        _chunks.Add(coordinate, generatedChunk);
        GeneratedChunkCount++;

        return generatedChunk;
    }

    public bool TryGetChunk(int x, int z, out Chunk? chunk)
    {
        return _chunks.TryGetValue(new ChunkCoordinate(x, z), out chunk);
    }

    public IEnumerable<(Chunk Chunk, int SubchunkIndex)> EnumerateDirtySubchunks()
    {
        foreach (var (chunk, subchunkIndex) in EnumerateDirtyAndBuiltSubchunkOrder())
        {
            if (chunk.GetSubchunk(subchunkIndex).IsDirty)
            {
                yield return (chunk, subchunkIndex);
            }
        }
    }

    public bool TryClaimNextDirtySubchunk(out Chunk? chunk, out int subchunkIndex)
    {
        foreach (var (candidateChunk, candidateSubchunkIndex) in EnumerateDirtySubchunks())
        {
            var candidateSubchunk = candidateChunk.GetSubchunk(candidateSubchunkIndex);
            if (candidateSubchunk.MeshState != SubchunkMeshState.NeedsBuild)
            {
                continue;
            }

            candidateSubchunk.MarkMeshQueued();
            chunk = candidateChunk;
            subchunkIndex = candidateSubchunkIndex;
            return true;
        }

        chunk = null;
        subchunkIndex = -1;
        return false;
    }

    public bool TryClaimNextDirtySubchunkSnapshot(out SubchunkMeshingSnapshot? snapshot)
    {
        foreach (var (candidateChunk, candidateSubchunkIndex) in EnumerateDirtySubchunks())
        {
            var candidateSubchunk = candidateChunk.GetSubchunk(candidateSubchunkIndex);
            if (candidateSubchunk.MeshState != SubchunkMeshState.NeedsBuild)
            {
                continue;
            }

            candidateSubchunk.MarkMeshQueued();
            snapshot = CreateSubchunkMeshingSnapshot(candidateChunk, candidateSubchunkIndex);
            return true;
        }

        snapshot = null;
        return false;
    }

    public bool TryBuildSubchunkMeshingSnapshot(int chunkX, int chunkZ, int subchunkIndex, out SubchunkMeshingSnapshot? snapshot)
    {
        if (!_chunks.TryGetValue(new ChunkCoordinate(chunkX, chunkZ), out var chunk))
        {
            snapshot = null;
            return false;
        }

        var subchunk = chunk.GetSubchunk(subchunkIndex);
        if (!subchunk.IsDirty)
        {
            snapshot = null;
            return false;
        }

        snapshot = CreateSubchunkMeshingSnapshot(chunk, subchunkIndex);
        return true;
    }

    public bool TryMarkSubchunkMeshBuilt(int chunkX, int chunkZ, int subchunkIndex, int revision, int buildVersion)
    {
        if (!_chunks.TryGetValue(new ChunkCoordinate(chunkX, chunkZ), out var chunk))
        {
            return false;
        }

        var subchunk = chunk.GetSubchunk(subchunkIndex);
        if (subchunk.BuildVersion != buildVersion)
        {
            return false;
        }

        return subchunk.TryMarkMeshBuilt(revision);
    }

    public bool TryGetBuiltSubchunkMeshingOutput(int chunkX, int chunkZ, int subchunkIndex, out SubchunkMeshingOutput? output)
    {
        if (!_chunks.TryGetValue(new ChunkCoordinate(chunkX, chunkZ), out var chunk))
        {
            output = null;
            return false;
        }

        var subchunk = chunk.GetSubchunk(subchunkIndex);
        if (subchunk.IsDirty || subchunk.MeshState != SubchunkMeshState.Built || subchunk.CurrentMeshingOutput is null)
        {
            output = null;
            return false;
        }

        output = subchunk.CurrentMeshingOutput;
        return true;
    }

    public IEnumerable<SubchunkMeshingOutput> EnumerateBuiltSubchunkMeshingOutputs()
    {
        foreach (var (chunk, subchunkIndex) in EnumerateDirtyAndBuiltSubchunkOrder())
        {
            if (TryGetBuiltSubchunkMeshingOutput(chunk.Coordinate.X, chunk.Coordinate.Z, subchunkIndex, out var output))
            {
                yield return output!;
            }
        }
    }

    public bool TryDrainNextBuiltSubchunkMeshingOutput(out SubchunkMeshingOutput? output)
    {
        foreach (var (chunk, subchunkIndex) in EnumerateDirtyAndBuiltSubchunkOrder())
        {
            if (chunk.GetSubchunk(subchunkIndex).TryDrainMeshingOutput(out output))
            {
                return true;
            }
        }

        output = null;
        return false;
    }

    public bool TryCreateClaimedSubchunkMeshingOutput(
        SubchunkMeshingSnapshot claimedSnapshot,
        IReadOnlyList<VisibleBlockFace> visibleFaces,
        out SubchunkMeshingOutput? output)
    {
        if (!IsClaimedSubchunkMeshingSnapshotCurrent(claimedSnapshot))
        {
            output = null;
            return false;
        }

        output = new SubchunkMeshingOutput(
            claimedSnapshot.ChunkCoordinate,
            claimedSnapshot.SubchunkIndex,
            claimedSnapshot.Revision,
            claimedSnapshot.BuildVersion,
            visibleFaces);
        return true;
    }

    public bool TryCompleteSubchunkMeshing(SubchunkMeshingSnapshot claimedSnapshot, SubchunkMeshingOutput output)
    {
        if (!IsClaimedSubchunkMeshingSnapshotCurrent(claimedSnapshot))
        {
            return false;
        }

        if (output.ChunkCoordinate != claimedSnapshot.ChunkCoordinate
            || output.SubchunkIndex != claimedSnapshot.SubchunkIndex
            || output.Revision != claimedSnapshot.Revision
            || output.BuildVersion != claimedSnapshot.BuildVersion)
        {
            return false;
        }

        return TryCompleteSubchunkMeshing(output);
    }

    public bool TryCompleteSubchunkMeshing(SubchunkMeshingOutput output)
    {
        if (!_chunks.TryGetValue(output.ChunkCoordinate, out var chunk))
        {
            return false;
        }

        var subchunk = chunk.GetSubchunk(output.SubchunkIndex);
        if (subchunk.BuildVersion != output.BuildVersion)
        {
            return false;
        }

        return subchunk.TryAcceptMeshingOutput(output);
    }

    public void Shutdown()
    {
        _chunks.Clear();
        _persistedChunkCoordinates.Clear();
        GeneratedChunkCount = 0;
        _generator = null;
    }

    private void EnsureInitialized()
    {
        _generator ??= new WorldGenerator(Seed);
    }

    private static ChunkSaveSnapshot ExportChunkSnapshot(Chunk chunk)
    {
        var subchunks = chunk.Subchunks
            .Select(subchunk => new SubchunkSaveSnapshot(subchunk.AsSpan().ToArray()))
            .ToArray();

        return new ChunkSaveSnapshot(chunk.Coordinate.X, chunk.Coordinate.Z, chunk.State, subchunks);
    }

    private static List<Chunk> ValidateAndImportChunks(WorldSaveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot.Chunks);

        var importedChunks = new List<Chunk>(snapshot.Chunks.Count);
        var seenCoordinates = new HashSet<ChunkCoordinate>();

        foreach (var chunkSnapshot in snapshot.Chunks)
        {
            var chunk = ImportChunkSnapshot(chunkSnapshot);
            if (!seenCoordinates.Add(chunk.Coordinate))
            {
                throw new InvalidOperationException($"Duplicate chunk snapshot for {chunk.Coordinate}.");
            }

            importedChunks.Add(chunk);
        }

        return importedChunks;
    }

    private static Chunk ImportChunkSnapshot(ChunkSaveSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(snapshot.Subchunks);

        if (!Enum.IsDefined(snapshot.State))
        {
            throw new InvalidOperationException($"Chunk ({snapshot.X}, {snapshot.Z}) has invalid chunk state '{snapshot.State}'.");
        }

        if (snapshot.Subchunks.Count != ChunkConstants.SubchunkCountPerChunk)
        {
            throw new InvalidOperationException($"Chunk ({snapshot.X}, {snapshot.Z}) expected {ChunkConstants.SubchunkCountPerChunk} subchunks but got {snapshot.Subchunks.Count}.");
        }

        var chunk = new Chunk(new ChunkCoordinate(snapshot.X, snapshot.Z))
        {
            State = snapshot.State
        };

        for (var subchunkIndex = 0; subchunkIndex < snapshot.Subchunks.Count; subchunkIndex++)
        {
            var subchunkSnapshot = snapshot.Subchunks[subchunkIndex];
            ArgumentNullException.ThrowIfNull(subchunkSnapshot);
            ArgumentNullException.ThrowIfNull(subchunkSnapshot.Blocks);

            if (subchunkSnapshot.Blocks.Length != ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize)
            {
                throw new InvalidOperationException($"Chunk ({snapshot.X}, {snapshot.Z}) subchunk {subchunkIndex} expected {ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize} blocks but got {subchunkSnapshot.Blocks.Length}.");
            }

            if (subchunkSnapshot.Blocks.Any(blockId => !Enum.IsDefined(blockId)))
            {
                throw new InvalidOperationException($"Chunk ({snapshot.X}, {snapshot.Z}) subchunk {subchunkIndex} contains an invalid block id.");
            }

            chunk.GetSubchunk(subchunkIndex).RestoreBlocks(subchunkSnapshot.Blocks);
        }

        return chunk;
    }

    private IEnumerable<(Chunk Chunk, int SubchunkIndex)> EnumerateDirtyAndBuiltSubchunkOrder()
    {
        foreach (var chunk in _chunks.OrderBy(entry => entry.Key.X).ThenBy(entry => entry.Key.Z).Select(entry => entry.Value))
        {
            for (var subchunkIndex = 0; subchunkIndex < ChunkConstants.SubchunkCountPerChunk; subchunkIndex++)
            {
                yield return (chunk, subchunkIndex);
            }
        }
    }

    private bool IsClaimedSubchunkMeshingSnapshotCurrent(SubchunkMeshingSnapshot claimedSnapshot)
    {
        if (!_chunks.TryGetValue(claimedSnapshot.ChunkCoordinate, out var chunk))
        {
            return false;
        }

        var subchunk = chunk.GetSubchunk(claimedSnapshot.SubchunkIndex);
        return subchunk.MeshState == SubchunkMeshState.Queued
            && subchunk.Revision == claimedSnapshot.Revision
            && subchunk.BuildVersion == claimedSnapshot.BuildVersion;
    }

    private void TrackPersistedChunksForEdit(Chunk chunk, LocalBlockCoordinate localCoordinate)
    {
        _persistedChunkCoordinates.Add(chunk.Coordinate);

        if (localCoordinate.X == 0)
        {
            _persistedChunkCoordinates.Add(GetOrCreateChunk(chunk.Coordinate.X - 1, chunk.Coordinate.Z).Coordinate);
        }

        if (localCoordinate.X == ChunkConstants.ChunkSizeX - 1)
        {
            _persistedChunkCoordinates.Add(GetOrCreateChunk(chunk.Coordinate.X + 1, chunk.Coordinate.Z).Coordinate);
        }

        if (localCoordinate.Z == 0)
        {
            _persistedChunkCoordinates.Add(GetOrCreateChunk(chunk.Coordinate.X, chunk.Coordinate.Z - 1).Coordinate);
        }

        if (localCoordinate.Z == ChunkConstants.ChunkSizeZ - 1)
        {
            _persistedChunkCoordinates.Add(GetOrCreateChunk(chunk.Coordinate.X, chunk.Coordinate.Z + 1).Coordinate);
        }
    }

    private void MarkNeighborSubchunksDirty(Chunk chunk, LocalBlockCoordinate localCoordinate)
    {
        if (localCoordinate.X == 0)
        {
            GetOrCreateChunk(chunk.Coordinate.X - 1, chunk.Coordinate.Z).MarkSubchunkDirty(localCoordinate.SubchunkIndex);
        }

        if (localCoordinate.X == ChunkConstants.ChunkSizeX - 1)
        {
            GetOrCreateChunk(chunk.Coordinate.X + 1, chunk.Coordinate.Z).MarkSubchunkDirty(localCoordinate.SubchunkIndex);
        }

        if (localCoordinate.Z == 0)
        {
            GetOrCreateChunk(chunk.Coordinate.X, chunk.Coordinate.Z - 1).MarkSubchunkDirty(localCoordinate.SubchunkIndex);
        }

        if (localCoordinate.Z == ChunkConstants.ChunkSizeZ - 1)
        {
            GetOrCreateChunk(chunk.Coordinate.X, chunk.Coordinate.Z + 1).MarkSubchunkDirty(localCoordinate.SubchunkIndex);
        }

        if (localCoordinate.SubchunkLocalY == 0 && localCoordinate.SubchunkIndex > 0)
        {
            chunk.MarkSubchunkDirty(localCoordinate.SubchunkIndex - 1);
        }

        if (localCoordinate.SubchunkLocalY == ChunkConstants.SubchunkSize - 1 && localCoordinate.SubchunkIndex < ChunkConstants.SubchunkCountPerChunk - 1)
        {
            chunk.MarkSubchunkDirty(localCoordinate.SubchunkIndex + 1);
        }
    }

    private SubchunkMeshingSnapshot CreateSubchunkMeshingSnapshot(Chunk chunk, int subchunkIndex)
    {
        var subchunk = chunk.GetSubchunk(subchunkIndex);
        return new SubchunkMeshingSnapshot(
            chunk.Coordinate,
            subchunkIndex,
            subchunk.Revision,
            subchunk.BuildVersion,
            CopySubchunkBlocks(chunk, subchunkIndex),
            CopyNegativeXBorder(chunk, subchunkIndex),
            CopyPositiveXBorder(chunk, subchunkIndex),
            CopyNegativeYBorder(chunk, subchunkIndex),
            CopyPositiveYBorder(chunk, subchunkIndex),
            CopyNegativeZBorder(chunk, subchunkIndex),
            CopyPositiveZBorder(chunk, subchunkIndex));
    }

    private static BlockId[] CopySubchunkBlocks(Chunk chunk, int subchunkIndex)
    {
        return chunk.GetSubchunk(subchunkIndex).AsSpan().ToArray();
    }

    private BlockId[] CopyNegativeXBorder(Chunk chunk, int subchunkIndex)
    {
        var border = new BlockId[ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize];
        var minWorldX = chunk.Coordinate.OriginX - 1;
        var baseWorldY = subchunkIndex * ChunkConstants.SubchunkSize;
        var baseWorldZ = chunk.Coordinate.OriginZ;

        for (var localZ = 0; localZ < ChunkConstants.SubchunkSize; localZ++)
        {
            for (var localY = 0; localY < ChunkConstants.SubchunkSize; localY++)
            {
                border[localY + (localZ * ChunkConstants.SubchunkSize)] = GetBlockOrAirWithoutCreating(minWorldX, baseWorldY + localY, baseWorldZ + localZ);
            }
        }

        return border;
    }

    private BlockId[] CopyPositiveXBorder(Chunk chunk, int subchunkIndex)
    {
        var border = new BlockId[ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize];
        var maxWorldX = chunk.Coordinate.OriginX + ChunkConstants.ChunkSizeX;
        var baseWorldY = subchunkIndex * ChunkConstants.SubchunkSize;
        var baseWorldZ = chunk.Coordinate.OriginZ;

        for (var localZ = 0; localZ < ChunkConstants.SubchunkSize; localZ++)
        {
            for (var localY = 0; localY < ChunkConstants.SubchunkSize; localY++)
            {
                border[localY + (localZ * ChunkConstants.SubchunkSize)] = GetBlockOrAirWithoutCreating(maxWorldX, baseWorldY + localY, baseWorldZ + localZ);
            }
        }

        return border;
    }

    private BlockId[] CopyNegativeYBorder(Chunk chunk, int subchunkIndex)
    {
        var border = new BlockId[ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize];
        var worldY = (subchunkIndex * ChunkConstants.SubchunkSize) - 1;
        if (worldY < 0)
        {
            return border;
        }

        var baseWorldX = chunk.Coordinate.OriginX;
        var baseWorldZ = chunk.Coordinate.OriginZ;

        for (var localZ = 0; localZ < ChunkConstants.SubchunkSize; localZ++)
        {
            for (var localX = 0; localX < ChunkConstants.SubchunkSize; localX++)
            {
                border[localX + (localZ * ChunkConstants.SubchunkSize)] = GetBlockOrAirWithoutCreating(baseWorldX + localX, worldY, baseWorldZ + localZ);
            }
        }

        return border;
    }

    private BlockId[] CopyPositiveYBorder(Chunk chunk, int subchunkIndex)
    {
        var border = new BlockId[ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize];
        var worldY = ((subchunkIndex + 1) * ChunkConstants.SubchunkSize);
        if (worldY >= ChunkConstants.ChunkHeight)
        {
            return border;
        }

        var baseWorldX = chunk.Coordinate.OriginX;
        var baseWorldZ = chunk.Coordinate.OriginZ;

        for (var localZ = 0; localZ < ChunkConstants.SubchunkSize; localZ++)
        {
            for (var localX = 0; localX < ChunkConstants.SubchunkSize; localX++)
            {
                border[localX + (localZ * ChunkConstants.SubchunkSize)] = GetBlockOrAirWithoutCreating(baseWorldX + localX, worldY, baseWorldZ + localZ);
            }
        }

        return border;
    }

    private BlockId[] CopyNegativeZBorder(Chunk chunk, int subchunkIndex)
    {
        var border = new BlockId[ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize];
        var minWorldZ = chunk.Coordinate.OriginZ - 1;
        var baseWorldX = chunk.Coordinate.OriginX;
        var baseWorldY = subchunkIndex * ChunkConstants.SubchunkSize;

        for (var localY = 0; localY < ChunkConstants.SubchunkSize; localY++)
        {
            for (var localX = 0; localX < ChunkConstants.SubchunkSize; localX++)
            {
                border[localX + (localY * ChunkConstants.SubchunkSize)] = GetBlockOrAirWithoutCreating(baseWorldX + localX, baseWorldY + localY, minWorldZ);
            }
        }

        return border;
    }

    private BlockId[] CopyPositiveZBorder(Chunk chunk, int subchunkIndex)
    {
        var border = new BlockId[ChunkConstants.SubchunkSize * ChunkConstants.SubchunkSize];
        var maxWorldZ = chunk.Coordinate.OriginZ + ChunkConstants.ChunkSizeZ;
        var baseWorldX = chunk.Coordinate.OriginX;
        var baseWorldY = subchunkIndex * ChunkConstants.SubchunkSize;

        for (var localY = 0; localY < ChunkConstants.SubchunkSize; localY++)
        {
            for (var localX = 0; localX < ChunkConstants.SubchunkSize; localX++)
            {
                border[localX + (localY * ChunkConstants.SubchunkSize)] = GetBlockOrAirWithoutCreating(baseWorldX + localX, baseWorldY + localY, maxWorldZ);
            }
        }

        return border;
    }

    private BlockId GetBlockOrAirWithoutCreating(int worldX, int worldY, int worldZ)
    {
        return TryGetBlock(worldX, worldY, worldZ, out var blockId)
            ? blockId
            : BlockId.Air;
    }

    private static bool IsOccluding(BlockId blockId)
    {
        return blockId != BlockId.Air;
    }

    private static void ValidateWorldY(int worldY)
    {
        if ((uint)worldY >= ChunkConstants.ChunkHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(worldY));
        }
    }

    private (Chunk Chunk, LocalBlockCoordinate LocalCoordinate) ResolveWorldBlock(int worldX, int worldY, int worldZ)
    {
        var (chunk, localCoordinate, _) = ResolveWorldBlockWithCoordinate(worldX, worldY, worldZ);
        return (chunk, localCoordinate);
    }

    private (Chunk Chunk, LocalBlockCoordinate LocalCoordinate, ChunkCoordinate ChunkCoordinate) ResolveWorldBlockWithCoordinate(int worldX, int worldY, int worldZ)
    {
        ValidateWorldY(worldY);

        var chunkCoordinate = ChunkCoordinate.FromWorldBlock(worldX, worldZ);
        var localCoordinate = LocalBlockCoordinate.FromWorldBlock(worldX, worldY, worldZ);
        var chunk = GetOrCreateChunk(chunkCoordinate.X, chunkCoordinate.Z);

        return (chunk, localCoordinate, chunkCoordinate);
    }
}
