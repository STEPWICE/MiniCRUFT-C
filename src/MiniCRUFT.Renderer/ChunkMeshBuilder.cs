using System;
using System.Collections.Generic;
using System.Numerics;
using MiniCRUFT.Core;
using MiniCRUFT.World;

namespace MiniCRUFT.Renderer;

public sealed class ChunkMeshBuilder
{
    private const float TorchHalfWidth = 0.12f;
    private const float TorchHeight = 0.7f;
    private const float TorchWallOffset = 0.3f;
    private const float TorchWallLift = 0.2f;
    private const float TorchTilt = 0.3926991f; // ~22.5 deg
    private const float TorchFlameWidth = 0.35f;
    private const float TorchFlameHeight = 0.45f;
    private const float TorchFlameLift = 0.02f;
    private static readonly Vector2 TorchUvMin = new(7f / 16f, 6f / 16f);
    private static readonly Vector2 TorchUvMax = new(9f / 16f, 1f);

    private readonly TextureAtlas _atlas;
    private readonly RenderConfig _renderConfig;
    private readonly AtmosphereConfig _atmosphereConfig;
    private readonly Vector3 _waterTintLinear;
    private readonly Vector3 _waterShoreTintLinear;
    private readonly Vector3 _lavaTintLinear;
    private readonly float _waterShoreStrength;
    private readonly float _lavaOpacity;

    public ChunkMeshBuilder(TextureAtlas atlas, RenderConfig renderConfig, AtmosphereConfig atmosphereConfig)
    {
        _atlas = atlas;
        _renderConfig = renderConfig;
        _atmosphereConfig = atmosphereConfig;
        _waterTintLinear = ColorSpace.ToLinear(atmosphereConfig.WaterTint.ToVector3());
        _waterShoreTintLinear = ColorSpace.ToLinear(atmosphereConfig.WaterShoreTint.ToVector3());
        _lavaTintLinear = ColorSpace.ToLinear(atmosphereConfig.LavaTint.ToVector3());
        _waterShoreStrength = atmosphereConfig.WaterShoreStrength;
        _lavaOpacity = atmosphereConfig.LavaOpacity;
    }

    public MeshData Build(ChunkNeighborhood neighborhood)
    {
        var data = new MeshData();
        BuildGreedyPass(data, neighborhood, FacePass.Opaque);
        BuildGreedyPass(data, neighborhood, FacePass.Transparent);

        AddCrossGeometry(data, neighborhood);
        AddTorchGeometry(data, neighborhood);

        return data;
    }

    private enum FacePass
    {
        Opaque,
        Transparent
    }

    private void BuildGreedyPass(MeshData data, ChunkNeighborhood neighborhood, FacePass pass)
    {
        int[] dims = { Chunk.SizeX, Chunk.SizeY, Chunk.SizeZ };
        for (int d = 0; d < 3; d++)
        {
            int u = (d + 1) % 3;
            int v = (d + 2) % 3;
            int[] x = new int[3];
            int[] q = new int[3];
            q[d] = 1;

            var mask = new FaceCell[dims[u] * dims[v]];

            for (x[d] = -1; x[d] < dims[d]; )
            {
                int n = 0;
                for (x[v] = 0; x[v] < dims[v]; x[v]++)
                {
                    for (x[u] = 0; x[u] < dims[u]; x[u]++)
                    {
                        bool hasA = neighborhood.TryGetBlock(x[0], x[1], x[2], out var a);
                        bool hasB = neighborhood.TryGetBlock(x[0] + q[0], x[1] + q[1], x[2] + q[2], out var b);
                        if (!hasA)
                        {
                            a = BlockId.Air;
                        }
                        if (!hasB)
                        {
                            b = BlockId.Air;
                        }

                        var cell = FaceCell.Empty;
                        if (IsFaceVisible(pass, a, b))
                        {
                            int tintKey = GetTintKey(neighborhood, a, d, false, x[0], x[2]);
                            cell = new FaceCell(a, false, tintKey);
                        }
                        else if (IsFaceVisible(pass, b, a))
                        {
                            int tintKey = GetTintKey(neighborhood, b, d, true, x[0], x[2]);
                            cell = new FaceCell(b, true, tintKey);
                        }

                        mask[n++] = cell;
                    }
                }

                x[d]++;
                n = 0;

                for (int j = 0; j < dims[v]; j++)
                {
                    for (int i = 0; i < dims[u]; )
                    {
                        var cell = mask[n];
                        if (cell.IsEmpty)
                        {
                            i++;
                            n++;
                            continue;
                        }

                        int width;
                        for (width = 1; i + width < dims[u] && mask[n + width].Equals(cell); width++)
                        {
                        }

                        int height;
                        for (height = 1; j + height < dims[v]; height++)
                        {
                            bool done = false;
                            for (int k = 0; k < width; k++)
                            {
                                if (!mask[n + k + height * dims[u]].Equals(cell))
                                {
                                    done = true;
                                    break;
                                }
                            }
                            if (done)
                            {
                                break;
                            }
                        }

                        x[u] = i;
                        x[v] = j;

                        int[] du = new int[3];
                        int[] dv = new int[3];
                        du[u] = width;
                        dv[v] = height;

                        AddQuad(data, neighborhood, d, cell.BackFace, x, du, dv, cell.BlockId);

                        for (int h = 0; h < height; h++)
                        {
                            for (int w = 0; w < width; w++)
                            {
                                mask[n + w + h * dims[u]] = FaceCell.Empty;
                            }
                        }

                        i += width;
                        n += width;
                    }
                }
            }
        }
    }

    private static bool IsFaceVisible(FacePass pass, BlockId current, BlockId neighbor)
    {
        if (current == BlockId.Air)
        {
            return false;
        }

        var currentDef = BlockRegistry.Get(current);
        if (currentDef.RenderMode == RenderMode.Cross || currentDef.RenderMode == RenderMode.Torch)
        {
            return false;
        }

        if (LiquidBlocks.IsLiquid(current) && LiquidBlocks.IsLiquid(neighbor))
        {
            return false;
        }

        if (pass == FacePass.Opaque)
        {
            if (currentDef.RenderMode != RenderMode.Opaque)
            {
                return false;
            }

            if (neighbor == BlockId.Air)
            {
                return true;
            }

            var neighborDef = BlockRegistry.Get(neighbor);
            return neighborDef.RenderMode != RenderMode.Opaque;
        }

        if (currentDef.RenderMode == RenderMode.Opaque)
        {
            return false;
        }

        if (neighbor == BlockId.Air)
        {
            return true;
        }

        return neighbor != current;
    }

    private void AddQuad(MeshData data, ChunkNeighborhood neighborhood, int axis, bool backFace, int[] x, int[] du, int[] dv, BlockId block)
    {
        var def = BlockRegistry.Get(block);
        var face = GetFace(axis, backFace);
        string textureName = def.GetTextureName(face);
        var region = _atlas.GetRegion(textureName);

        var chunk = neighborhood.Center;
        var basePos = new Vector3(
            chunk.ChunkX * Chunk.SizeX + x[0],
            x[1],
            chunk.ChunkZ * Chunk.SizeZ + x[2]);

        Vector3 duVec = new(du[0], du[1], du[2]);
        Vector3 dvVec = new(dv[0], dv[1], dv[2]);

        Vector3 normal = axis switch
        {
            0 => backFace ? -Vector3.UnitX : Vector3.UnitX,
            1 => backFace ? -Vector3.UnitY : Vector3.UnitY,
            _ => backFace ? -Vector3.UnitZ : Vector3.UnitZ
        };

        var renderMode = def.RenderMode;
        var (vertices, indices) = renderMode switch
        {
            RenderMode.Transparent => (data.TransparentVertices, data.TransparentIndices),
            RenderMode.Cutout => (data.CutoutVertices, data.CutoutIndices),
            RenderMode.Cross => (data.CutoutVertices, data.CutoutIndices),
            RenderMode.Torch => (data.CutoutVertices, data.CutoutIndices),
            _ => (data.SolidVertices, data.SolidIndices)
        };

        uint start = (uint)vertices.Count;

        Vector3 v0 = basePos;
        Vector3 v1 = basePos + duVec;
        Vector3 v2 = basePos + duVec + dvVec;
        Vector3 v3 = basePos + dvVec;

        GetUvAxes(axis, out var uAxis, out var vAxis);
        Vector2 uv0 = ProjectUv(v0, basePos, uAxis, vAxis);
        Vector2 uv1 = ProjectUv(v1, basePos, uAxis, vAxis);
        Vector2 uv2 = ProjectUv(v2, basePos, uAxis, vAxis);
        Vector2 uv3 = ProjectUv(v3, basePos, uAxis, vAxis);

        if (backFace)
        {
            (v1, v3) = (v3, v1);
            (uv1, uv3) = (uv3, uv1);
        }

        float ao0 = ComputeVertexAo(neighborhood, x[0], x[1], x[2], axis, backFace, -1, -1);
        float ao1 = ComputeVertexAo(neighborhood, x[0] + du[0], x[1] + du[1], x[2] + du[2], axis, backFace, 1, -1);
        float ao2 = ComputeVertexAo(neighborhood, x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2], axis, backFace, 1, 1);
        float ao3 = ComputeVertexAo(neighborhood, x[0] + dv[0], x[1] + dv[1], x[2] + dv[2], axis, backFace, -1, 1);

        float l0 = SampleVertexLight(neighborhood, x[0], x[1], x[2], axis, backFace) * ao0;
        float l1 = SampleVertexLight(neighborhood, x[0] + du[0], x[1] + du[1], x[2] + du[2], axis, backFace) * ao1;
        float l2 = SampleVertexLight(neighborhood, x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2], axis, backFace) * ao2;
        float l3 = SampleVertexLight(neighborhood, x[0] + dv[0], x[1] + dv[1], x[2] + dv[2], axis, backFace) * ao3;
        float faceLight = (l0 + l1 + l2 + l3) * 0.25f;

        Vector4 tint = GetTintColor(neighborhood, def, axis, backFace, x[0], x[2]);
        bool isWater = LiquidBlocks.IsWater(block);
        bool isLava = LiquidBlocks.IsLava(block);
        if (isWater)
        {
            Vector3 tintRgb = _waterTintLinear;
            if (axis == 1 && !backFace && IsWaterShore(neighborhood, x[0], x[1], x[2]))
            {
                tintRgb = Vector3.Lerp(_waterTintLinear, _waterShoreTintLinear, _waterShoreStrength);
            }
            tint = new Vector4(tintRgb, _atmosphereConfig.WaterOpacity);
        }
        else if (isLava)
        {
            tint = new Vector4(_lavaTintLinear, _lavaOpacity);
        }
        float materialId = def.IsFoliage ? RenderMaterial.Foliage :
            isWater ? RenderMaterial.Water :
            isLava ? RenderMaterial.Lava :
            block == BlockId.Sand ? RenderMaterial.Sand :
            block == BlockId.Stone ? RenderMaterial.Stone :
            def.RenderMode == RenderMode.Transparent || def.RenderMode == RenderMode.Cutout || def.RenderMode == RenderMode.Torch ? RenderMaterial.Transparent : RenderMaterial.Default;
        Vector2 atlasMin = region.Min;
        Vector2 atlasSize = region.Max - region.Min;

        if (NeedsWindingFlip(v0, v1, v2, normal))
        {
            (v1, v3) = (v3, v1);
            (uv1, uv3) = (uv3, uv1);
        }

        vertices.Add(new VoxelVertex(v0, normal, uv0, atlasMin, atlasSize, faceLight, tint, materialId));
        vertices.Add(new VoxelVertex(v1, normal, uv1, atlasMin, atlasSize, faceLight, tint, materialId));
        vertices.Add(new VoxelVertex(v2, normal, uv2, atlasMin, atlasSize, faceLight, tint, materialId));
        vertices.Add(new VoxelVertex(v3, normal, uv3, atlasMin, atlasSize, faceLight, tint, materialId));

        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start);
        indices.Add(start + 2);
        indices.Add(start + 3);
    }

    private static float SampleVertexLight(ChunkNeighborhood neighborhood, int vx, int vy, int vz, int axis, bool backFace)
    {
        int offset = backFace ? -1 : 1;
        int sx = vx + (axis == 0 ? offset : 0);
        int sy = vy + (axis == 1 ? offset : 0);
        int sz = vz + (axis == 2 ? offset : 0);

        byte sky = neighborhood.GetSkyLight(sx, sy, sz);
        byte torch = neighborhood.GetTorchLight(sx, sy, sz);
        byte light = (byte)Math.Clamp(sky + torch, 0, 15);
        return light / 15f;
    }

    private float ComputeVertexAo(ChunkNeighborhood neighborhood, int vx, int vy, int vz, int axis, bool backFace, int uDir, int vDir)
    {
        int normalOffset = backFace ? -1 : 1;
        int uAxis = axis == 0 ? 1 : 0;
        int vAxis = axis == 2 ? 1 : 2;
        if (axis == 1)
        {
            uAxis = 0;
            vAxis = 2;
        }

        int ox = vx + (axis == 0 ? normalOffset : 0);
        int oy = vy + (axis == 1 ? normalOffset : 0);
        int oz = vz + (axis == 2 ? normalOffset : 0);

        int s1x = ox + (uAxis == 0 ? uDir : 0);
        int s1y = oy + (uAxis == 1 ? uDir : 0);
        int s1z = oz + (uAxis == 2 ? uDir : 0);

        int s2x = ox + (vAxis == 0 ? vDir : 0);
        int s2y = oy + (vAxis == 1 ? vDir : 0);
        int s2z = oz + (vAxis == 2 ? vDir : 0);

        int cx = ox + (uAxis == 0 ? uDir : 0) + (vAxis == 0 ? vDir : 0);
        int cy = oy + (uAxis == 1 ? uDir : 0) + (vAxis == 1 ? vDir : 0);
        int cz = oz + (uAxis == 2 ? uDir : 0) + (vAxis == 2 ? vDir : 0);

        bool side1 = IsOccluder(neighborhood.GetBlock(s1x, s1y, s1z));
        bool side2 = IsOccluder(neighborhood.GetBlock(s2x, s2y, s2z));
        bool corner = IsOccluder(neighborhood.GetBlock(cx, cy, cz));

        return LightModel.ComputeAmbientOcclusion(side1, side2, corner, _renderConfig.AmbientOcclusionMin, _renderConfig.AmbientOcclusionStrength);
    }

    private static bool IsOccluder(BlockId id)
    {
        var def = BlockRegistry.Get(id);
        return def.BlocksSkyLight;
    }

    private static BlockFace GetFace(int axis, bool backFace)
    {
        return axis switch
        {
            1 => backFace ? BlockFace.Bottom : BlockFace.Top,
            0 => backFace ? BlockFace.West : BlockFace.East,
            _ => backFace ? BlockFace.North : BlockFace.South
        };
    }

    private static void GetUvAxes(int axis, out Vector3 uAxis, out Vector3 vAxis)
    {
        switch (axis)
        {
            case 0:
                uAxis = Vector3.UnitZ;
                vAxis = Vector3.UnitY;
                break;
            case 1:
                uAxis = Vector3.UnitX;
                vAxis = Vector3.UnitZ;
                break;
            default:
                uAxis = Vector3.UnitX;
                vAxis = Vector3.UnitY;
                break;
        }
    }

    private static Vector2 ProjectUv(Vector3 position, Vector3 origin, Vector3 uAxis, Vector3 vAxis)
    {
        var relative = position - origin;
        return new Vector2(Vector3.Dot(relative, uAxis), Vector3.Dot(relative, vAxis));
    }

    private readonly struct FaceCell
    {
        public readonly BlockId BlockId;
        public readonly bool BackFace;
        public readonly int TintKey;
        public readonly bool IsEmpty;

        public FaceCell(BlockId blockId, bool backFace, int tintKey)
        {
            BlockId = blockId;
            BackFace = backFace;
            TintKey = tintKey;
            IsEmpty = false;
        }

        private FaceCell(bool empty)
        {
            BlockId = BlockId.Air;
            BackFace = false;
            IsEmpty = empty;
        }

        public static FaceCell Empty => new(true);

        public bool Equals(FaceCell other) => IsEmpty == other.IsEmpty && BlockId == other.BlockId && BackFace == other.BackFace && TintKey == other.TintKey;
    }

    private static int GetTintKey(ChunkNeighborhood neighborhood, BlockId blockId, int axis, bool backFace, int x, int z)
    {
        var def = BlockRegistry.Get(blockId);
        if (def.TintType == TintType.None)
        {
            return 0;
        }

        int bx = x + (axis == 0 && backFace ? 1 : 0);
        int bz = z + (axis == 2 && backFace ? 1 : 0);
        var biome = neighborhood.GetBiome(bx, bz);
        return ((int)biome << 4) | (int)def.TintType;
    }

    private Vector4 GetTintColor(ChunkNeighborhood neighborhood, BlockDefinition def, int axis, bool backFace, int x, int z)
    {
        if (def.TintType == TintType.None)
        {
            return Vector4.One;
        }

        int bx = x + (axis == 0 && backFace ? 1 : 0);
        int bz = z + (axis == 2 && backFace ? 1 : 0);
        var biome = neighborhood.GetBiome(bx, bz);

        float strength = def.IsFoliage ? _renderConfig.Foliage.TintStrength : _renderConfig.BiomeTintStrength;
        Vector4 baseTint = def.TintType switch
        {
            TintType.Grass => new Vector4(BiomeRegistry.GetGrassColor(biome), 1f),
            TintType.Foliage => new Vector4(BiomeRegistry.GetFoliageColor(biome), 1f),
            _ => Vector4.One
        };
        var tintLinear = ColorSpace.ToLinear(new Vector3(baseTint.X, baseTint.Y, baseTint.Z));
        var rgb = Vector3.Lerp(Vector3.One, tintLinear, strength);
        return new Vector4(rgb, baseTint.W);
    }

    private static bool NeedsWindingFlip(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 normal)
    {
        var faceNormal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
        return Vector3.Dot(faceNormal, normal) < 0f;
    }

    private static bool IsWaterShore(ChunkNeighborhood neighborhood, int x, int y, int z)
    {
        if (y < 0 || y >= Chunk.SizeY)
        {
            return false;
        }

        return IsSolidNeighbor(neighborhood.GetBlock(x - 1, y, z)) ||
               IsSolidNeighbor(neighborhood.GetBlock(x + 1, y, z)) ||
               IsSolidNeighbor(neighborhood.GetBlock(x, y, z - 1)) ||
               IsSolidNeighbor(neighborhood.GetBlock(x, y, z + 1));
    }

    private static bool IsSolidNeighbor(BlockId id)
    {
        if (LiquidBlocks.IsLiquid(id) || id == BlockId.Air)
        {
            return false;
        }

        var def = BlockRegistry.Get(id);
        return def.IsSolid && !def.IsTransparent;
    }

    private static bool IsCross(BlockId id)
    {
        if (id == BlockId.Air)
        {
            return false;
        }

        return BlockRegistry.Get(id).RenderMode == RenderMode.Cross;
    }

    private static bool IsTorch(BlockId id)
    {
        if (id == BlockId.Air)
        {
            return false;
        }

        return BlockRegistry.Get(id).RenderMode == RenderMode.Torch;
    }

    private void AddCrossGeometry(MeshData data, ChunkNeighborhood neighborhood)
    {
        var chunk = neighborhood.Center;
        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                for (int y = 0; y < Chunk.SizeY; y++)
                {
                    var id = chunk.GetBlock(x, y, z);
                    if (id == BlockId.Fire)
                    {
                        AddFireGeometry(data, neighborhood, x, y, z);
                        continue;
                    }

                    if (!IsCross(id))
                    {
                        continue;
                    }

                    var def = BlockRegistry.Get(id);
                    string textureName = def.TextureSide;
                    var region = _atlas.GetRegion(textureName);

                    Vector3 basePos = new Vector3(
                        chunk.ChunkX * Chunk.SizeX + x,
                        y,
                        chunk.ChunkZ * Chunk.SizeZ + z);

                    float light = SampleCrossLight(neighborhood, x, y, z);
                    var tint = GetTintColor(neighborhood, def, 1, false, x, z);
                    float materialId = def.IsFoliage ? RenderMaterial.Foliage : RenderMaterial.Transparent;

                    Vector2 uv0 = Vector2.Zero;
                    Vector2 uv1 = new Vector2(1f, 0f);
                    Vector2 uv2 = Vector2.One;
                    Vector2 uv3 = new Vector2(0f, 1f);

                    Vector2 atlasMin = region.Min;
                    Vector2 atlasSize = region.Max - region.Min;
                    var normal = Vector3.UnitY;

                    AddCrossQuad(data.CutoutVertices, data.CutoutIndices,
                        basePos + new Vector3(0f, 0f, 0f),
                        basePos + new Vector3(1f, 0f, 1f),
                        basePos + new Vector3(1f, 1f, 1f),
                        basePos + new Vector3(0f, 1f, 0f),
                        uv0, uv1, uv2, uv3,
                        atlasMin, atlasSize, light, tint, normal, materialId);

                    AddCrossQuad(data.CutoutVertices, data.CutoutIndices,
                        basePos + new Vector3(1f, 0f, 0f),
                        basePos + new Vector3(0f, 0f, 1f),
                        basePos + new Vector3(0f, 1f, 1f),
                        basePos + new Vector3(1f, 1f, 0f),
                        uv0, uv1, uv2, uv3,
                        atlasMin, atlasSize, light, tint, normal, materialId);
                }
            }
        }
    }

    private void AddFireGeometry(MeshData data, ChunkNeighborhood neighborhood, int x, int y, int z)
    {
        var chunk = neighborhood.Center;
        var basePos = new Vector3(
            chunk.ChunkX * Chunk.SizeX + x,
            y,
            chunk.ChunkZ * Chunk.SizeZ + z);

        float light = SampleCrossLight(neighborhood, x, y, z);
        var fire0 = _atlas.GetRegion("fire_0");
        var fire1 = _atlas.GetRegion("fire_1");
        Vector4 tint = Vector4.One;
        float materialId = RenderMaterial.Torch;
        float height = 1.35f;
        float inset = 0.18f;

        Vector2 uv0 = Vector2.Zero;
        Vector2 uv1 = new Vector2(1f, 0f);
        Vector2 uv2 = Vector2.One;
        Vector2 uv3 = new Vector2(0f, 1f);
        var normal = Vector3.UnitY;

        AddCrossQuad(data.CutoutVertices, data.CutoutIndices,
            basePos + new Vector3(inset, 0f, inset),
            basePos + new Vector3(1f - inset, 0f, 1f - inset),
            basePos + new Vector3(1f - inset, height, 1f - inset),
            basePos + new Vector3(inset, height, inset),
            uv0, uv1, uv2, uv3,
            fire0.Min, fire0.Max - fire0.Min, light, tint, normal, materialId);

        AddCrossQuad(data.CutoutVertices, data.CutoutIndices,
            basePos + new Vector3(1f - inset, 0f, inset),
            basePos + new Vector3(inset, 0f, 1f - inset),
            basePos + new Vector3(inset, height * 0.92f, 1f - inset),
            basePos + new Vector3(1f - inset, height * 0.92f, inset),
            uv0, uv1, uv2, uv3,
            fire1.Min, fire1.Max - fire1.Min, light, tint, normal, materialId);
    }

    private static void AddCrossQuad(
        List<VoxelVertex> vertices,
        List<uint> indices,
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        Vector3 v3,
        Vector2 uv0,
        Vector2 uv1,
        Vector2 uv2,
        Vector2 uv3,
        Vector2 atlasMin,
        Vector2 atlasSize,
        float light,
        Vector4 tint,
        Vector3 normal,
        float materialId)
    {
        uint start = (uint)vertices.Count;
        vertices.Add(new VoxelVertex(v0, normal, uv0, atlasMin, atlasSize, light, tint, materialId));
        vertices.Add(new VoxelVertex(v1, normal, uv1, atlasMin, atlasSize, light, tint, materialId));
        vertices.Add(new VoxelVertex(v2, normal, uv2, atlasMin, atlasSize, light, tint, materialId));
        vertices.Add(new VoxelVertex(v3, normal, uv3, atlasMin, atlasSize, light, tint, materialId));

        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start);
        indices.Add(start + 2);
        indices.Add(start + 3);
    }

    private static float SampleCrossLight(ChunkNeighborhood neighborhood, int x, int y, int z)
    {
        byte sky = neighborhood.GetSkyLight(x, y, z);
        byte torch = neighborhood.GetTorchLight(x, y, z);
        byte light = (byte)Math.Clamp(sky + torch, 0, 15);
        return light / 15f;
    }

    private void AddTorchGeometry(MeshData data, ChunkNeighborhood neighborhood)
    {
        var chunk = neighborhood.Center;
        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                for (int y = 0; y < Chunk.SizeY; y++)
                {
                    var id = chunk.GetBlock(x, y, z);
                    if (!IsTorch(id))
                    {
                        continue;
                    }

                    var def = BlockRegistry.Get(id);
                    var region = _atlas.GetRegion(def.TextureSide);

                    Vector3 origin = new Vector3(
                        chunk.ChunkX * Chunk.SizeX + x,
                        y,
                        chunk.ChunkZ * Chunk.SizeZ + z);

                    float light = SampleCrossLight(neighborhood, x, y, z);
                    Vector4 tint = Vector4.One;
                    float materialId = RenderMaterial.Torch;

                    var mount = GetTorchMount(id);
                    var corners = BuildTorchCorners(origin, mount);

                    Vector2 uv0 = TorchUvMin;
                    Vector2 uv1 = new(TorchUvMax.X, TorchUvMin.Y);
                    Vector2 uv2 = TorchUvMax;
                    Vector2 uv3 = new(TorchUvMin.X, TorchUvMax.Y);
                    Vector2 atlasMin = region.Min;
                    Vector2 atlasSize = region.Max - region.Min;

                    AddTorchFace(data.CutoutVertices, data.CutoutIndices, corners[0], corners[1], corners[2], corners[3], uv0, uv1, uv2, uv3, atlasMin, atlasSize, light, tint, materialId);
                    AddTorchFace(data.CutoutVertices, data.CutoutIndices, corners[5], corners[4], corners[7], corners[6], uv0, uv1, uv2, uv3, atlasMin, atlasSize, light, tint, materialId);
                    AddTorchFace(data.CutoutVertices, data.CutoutIndices, corners[4], corners[0], corners[3], corners[7], uv0, uv1, uv2, uv3, atlasMin, atlasSize, light, tint, materialId);
                    AddTorchFace(data.CutoutVertices, data.CutoutIndices, corners[1], corners[5], corners[6], corners[2], uv0, uv1, uv2, uv3, atlasMin, atlasSize, light, tint, materialId);
                    AddTorchFace(data.CutoutVertices, data.CutoutIndices, corners[3], corners[2], corners[6], corners[7], uv0, uv1, uv2, uv3, atlasMin, atlasSize, light, tint, materialId);

                    var flameRegion = _atlas.GetRegion("fire_0");
                    AddTorchFlame(data, corners, flameRegion, light);
                }
            }
        }
    }

    private static void AddTorchFace(
        List<VoxelVertex> vertices,
        List<uint> indices,
        Vector3 v0,
        Vector3 v1,
        Vector3 v2,
        Vector3 v3,
        Vector2 uv0,
        Vector2 uv1,
        Vector2 uv2,
        Vector2 uv3,
        Vector2 atlasMin,
        Vector2 atlasSize,
        float light,
        Vector4 tint,
        float materialId)
    {
        Vector3 normal = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v0));
        uint start = (uint)vertices.Count;
        vertices.Add(new VoxelVertex(v0, normal, uv0, atlasMin, atlasSize, light, tint, materialId));
        vertices.Add(new VoxelVertex(v1, normal, uv1, atlasMin, atlasSize, light, tint, materialId));
        vertices.Add(new VoxelVertex(v2, normal, uv2, atlasMin, atlasSize, light, tint, materialId));
        vertices.Add(new VoxelVertex(v3, normal, uv3, atlasMin, atlasSize, light, tint, materialId));

        indices.Add(start);
        indices.Add(start + 1);
        indices.Add(start + 2);
        indices.Add(start);
        indices.Add(start + 2);
        indices.Add(start + 3);
    }

    private static Vector3[] BuildTorchCorners(Vector3 origin, TorchMount mount)
    {
        var local = new[]
        {
            new Vector3(-TorchHalfWidth, 0f, -TorchHalfWidth),
            new Vector3(TorchHalfWidth, 0f, -TorchHalfWidth),
            new Vector3(TorchHalfWidth, TorchHeight, -TorchHalfWidth),
            new Vector3(-TorchHalfWidth, TorchHeight, -TorchHalfWidth),
            new Vector3(-TorchHalfWidth, 0f, TorchHalfWidth),
            new Vector3(TorchHalfWidth, 0f, TorchHalfWidth),
            new Vector3(TorchHalfWidth, TorchHeight, TorchHalfWidth),
            new Vector3(-TorchHalfWidth, TorchHeight, TorchHalfWidth)
        };

        Quaternion rotation = Quaternion.Identity;
        Vector3 anchor = new Vector3(0.5f, 0f, 0.5f);
        switch (mount)
        {
            case TorchMount.WallNorth:
                anchor = new Vector3(0.5f, TorchWallLift, 0.5f - TorchWallOffset);
                rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, TorchTilt);
                break;
            case TorchMount.WallSouth:
                anchor = new Vector3(0.5f, TorchWallLift, 0.5f + TorchWallOffset);
                rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -TorchTilt);
                break;
            case TorchMount.WallWest:
                anchor = new Vector3(0.5f - TorchWallOffset, TorchWallLift, 0.5f);
                rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -TorchTilt);
                break;
            case TorchMount.WallEast:
                anchor = new Vector3(0.5f + TorchWallOffset, TorchWallLift, 0.5f);
                rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, TorchTilt);
                break;
        }

        var corners = new Vector3[local.Length];
        for (int i = 0; i < local.Length; i++)
        {
            corners[i] = origin + anchor + Vector3.Transform(local[i], rotation);
        }

        return corners;
    }

    private static void AddTorchFlame(MeshData data, Vector3[] corners, AtlasRegion region, float light)
    {
        Vector3 topCenter = (corners[2] + corners[3] + corners[6] + corners[7]) * 0.25f;
        Vector3 basePos = topCenter + new Vector3(0f, TorchFlameLift, 0f);
        float half = TorchFlameWidth * 0.5f;
        float height = TorchFlameHeight;

        Vector2 uv0 = Vector2.Zero;
        Vector2 uv1 = new Vector2(1f, 0f);
        Vector2 uv2 = Vector2.One;
        Vector2 uv3 = new Vector2(0f, 1f);
        Vector2 atlasMin = region.Min;
        Vector2 atlasSize = region.Max - region.Min;
        Vector4 tint = Vector4.One;
        float materialId = RenderMaterial.Torch;
        var normal = Vector3.UnitY;

        AddCrossQuad(data.CutoutVertices, data.CutoutIndices,
            basePos + new Vector3(-half, 0f, -half),
            basePos + new Vector3(half, 0f, half),
            basePos + new Vector3(half, height, half),
            basePos + new Vector3(-half, height, -half),
            uv0, uv1, uv2, uv3,
            atlasMin, atlasSize, light, tint, normal, materialId);

        AddCrossQuad(data.CutoutVertices, data.CutoutIndices,
            basePos + new Vector3(half, 0f, -half),
            basePos + new Vector3(-half, 0f, half),
            basePos + new Vector3(-half, height, half),
            basePos + new Vector3(half, height, -half),
            uv0, uv1, uv2, uv3,
            atlasMin, atlasSize, light, tint, normal, materialId);
    }

    private static TorchMount GetTorchMount(BlockId id)
    {
        return id switch
        {
            BlockId.TorchWallNorth => TorchMount.WallNorth,
            BlockId.TorchWallSouth => TorchMount.WallSouth,
            BlockId.TorchWallWest => TorchMount.WallWest,
            BlockId.TorchWallEast => TorchMount.WallEast,
            _ => TorchMount.Floor
        };
    }

    private enum TorchMount
    {
        Floor,
        WallNorth,
        WallSouth,
        WallWest,
        WallEast
    }
}
