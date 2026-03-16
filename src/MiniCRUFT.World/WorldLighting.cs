using System.Collections.Generic;

namespace MiniCRUFT.World;

public static class WorldLighting
{
    public static void RecalculateChunkLighting(Chunk chunk)
    {
        RecalculateChunkLighting(chunk, null, null, null, null);
    }

    public static void RecalculateChunkLighting(Chunk chunk, Chunk? north, Chunk? south, Chunk? east, Chunk? west)
    {
        var sky = chunk.SkyLight;
        var torch = chunk.TorchLight;
        System.Array.Clear(sky, 0, sky.Length);
        System.Array.Clear(torch, 0, torch.Length);

        var skyQueue = new Queue<(int x, int y, int z)>();
        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                bool blocked = false;
                for (int y = Chunk.SizeY - 1; y >= 0; y--)
                {
                    int index = Chunk.GetIndex(x, y, z);
                    var block = chunk.Blocks[index];
                    var def = BlockRegistry.Get(block);
                    if (!blocked && !def.BlocksSkyLight)
                    {
                        sky[index] = 15;
                        skyQueue.Enqueue((x, y, z));
                    }
                    else
                    {
                        sky[index] = 0;
                    }

                    if (def.BlocksSkyLight)
                    {
                        blocked = true;
                    }
                }
            }
        }

        SeedSkyFromNeighbors(chunk, north, south, east, west, skyQueue);

        while (skyQueue.Count > 0)
        {
            var (x, y, z) = skyQueue.Dequeue();
            byte light = chunk.GetSkyLight(x, y, z);
            if (light <= 1)
            {
                continue;
            }

            TryPropagateSky(chunk, skyQueue, x + 1, y, z, light);
            TryPropagateSky(chunk, skyQueue, x - 1, y, z, light);
            TryPropagateSky(chunk, skyQueue, x, y + 1, z, light);
            TryPropagateSky(chunk, skyQueue, x, y - 1, z, light);
            TryPropagateSky(chunk, skyQueue, x, y, z + 1, light);
            TryPropagateSky(chunk, skyQueue, x, y, z - 1, light);
        }

        var queue = new Queue<(int x, int y, int z)>();
        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                for (int y = 0; y < Chunk.SizeY; y++)
                {
                    var block = chunk.GetBlock(x, y, z);
                    var def = BlockRegistry.Get(block);
                    if (def.LightEmission > 0)
                    {
                        chunk.SetTorchLight(x, y, z, def.LightEmission);
                        queue.Enqueue((x, y, z));
                    }
                }
            }
        }

        SeedTorchFromNeighbors(chunk, north, south, east, west, queue);

        while (queue.Count > 0)
        {
            var (x, y, z) = queue.Dequeue();
            byte light = chunk.GetTorchLight(x, y, z);
            if (light <= 1)
            {
                continue;
            }

            TryPropagate(chunk, queue, x + 1, y, z, light);
            TryPropagate(chunk, queue, x - 1, y, z, light);
            TryPropagate(chunk, queue, x, y + 1, z, light);
            TryPropagate(chunk, queue, x, y - 1, z, light);
            TryPropagate(chunk, queue, x, y, z + 1, light);
            TryPropagate(chunk, queue, x, y, z - 1, light);
        }

        chunk.ClearLightingDirty();
    }

    private static void TryPropagateSky(Chunk chunk, Queue<(int x, int y, int z)> queue, int x, int y, int z, byte current)
    {
        if (x < 0 || x >= Chunk.SizeX || z < 0 || z >= Chunk.SizeZ || y < 0 || y >= Chunk.SizeY)
        {
            return;
        }

        var block = chunk.GetBlock(x, y, z);
        var def = BlockRegistry.Get(block);
        if (def.BlocksSkyLight)
        {
            return;
        }

        byte next = (byte)(current - 1);
        if (next <= chunk.GetSkyLight(x, y, z))
        {
            return;
        }

        chunk.SetSkyLight(x, y, z, next);
        queue.Enqueue((x, y, z));
    }

    private static void TryPropagate(Chunk chunk, Queue<(int x, int y, int z)> queue, int x, int y, int z, byte current)
    {
        if (x < 0 || x >= Chunk.SizeX || z < 0 || z >= Chunk.SizeZ || y < 0 || y >= Chunk.SizeY)
        {
            return;
        }

        var block = chunk.GetBlock(x, y, z);
        var def = BlockRegistry.Get(block);
        if (def.IsSolid && !def.IsTransparent)
        {
            return;
        }

        byte next = (byte)(current - 1);
        if (next <= chunk.GetTorchLight(x, y, z))
        {
            return;
        }

        chunk.SetTorchLight(x, y, z, next);
        queue.Enqueue((x, y, z));
    }

    private static void SeedSkyFromNeighbors(Chunk chunk, Chunk? north, Chunk? south, Chunk? east, Chunk? west, Queue<(int x, int y, int z)> queue)
    {
        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int y = 0; y < Chunk.SizeY; y++)
            {
                SeedSkyEdge(chunk, north, x, y, 0, 0, -1, queue);
                SeedSkyEdge(chunk, south, x, y, Chunk.SizeZ - 1, 0, 1, queue);
            }
        }

        for (int z = 0; z < Chunk.SizeZ; z++)
        {
            for (int y = 0; y < Chunk.SizeY; y++)
            {
                SeedSkyEdge(chunk, west, 0, y, z, -1, 0, queue);
                SeedSkyEdge(chunk, east, Chunk.SizeX - 1, y, z, 1, 0, queue);
            }
        }
    }

    private static void SeedSkyEdge(Chunk chunk, Chunk? neighbor, int x, int y, int z, int dx, int dz, Queue<(int x, int y, int z)> queue)
    {
        if (neighbor == null)
        {
            return;
        }

        int nx = x + dx;
        int nz = z + dz;
        if (nx < 0) nx += Chunk.SizeX;
        if (nx >= Chunk.SizeX) nx -= Chunk.SizeX;
        if (nz < 0) nz += Chunk.SizeZ;
        if (nz >= Chunk.SizeZ) nz -= Chunk.SizeZ;

        var neighborBlock = neighbor.GetBlock(nx, y, nz);
        if (BlockRegistry.Get(neighborBlock).BlocksSkyLight)
        {
            return;
        }

        var currentBlock = chunk.GetBlock(x, y, z);
        if (BlockRegistry.Get(currentBlock).BlocksSkyLight)
        {
            return;
        }

        byte neighborLight = neighbor.GetSkyLight(nx, y, nz);
        if (neighborLight <= 1)
        {
            return;
        }

        byte next = (byte)(neighborLight - 1);
        if (next <= chunk.GetSkyLight(x, y, z))
        {
            return;
        }

        chunk.SetSkyLight(x, y, z, next);
        queue.Enqueue((x, y, z));
    }

    private static void SeedTorchFromNeighbors(Chunk chunk, Chunk? north, Chunk? south, Chunk? east, Chunk? west, Queue<(int x, int y, int z)> queue)
    {
        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int y = 0; y < Chunk.SizeY; y++)
            {
                SeedTorchEdge(chunk, north, x, y, 0, 0, -1, queue);
                SeedTorchEdge(chunk, south, x, y, Chunk.SizeZ - 1, 0, 1, queue);
            }
        }

        for (int z = 0; z < Chunk.SizeZ; z++)
        {
            for (int y = 0; y < Chunk.SizeY; y++)
            {
                SeedTorchEdge(chunk, west, 0, y, z, -1, 0, queue);
                SeedTorchEdge(chunk, east, Chunk.SizeX - 1, y, z, 1, 0, queue);
            }
        }
    }

    private static void SeedTorchEdge(Chunk chunk, Chunk? neighbor, int x, int y, int z, int dx, int dz, Queue<(int x, int y, int z)> queue)
    {
        if (neighbor == null)
        {
            return;
        }

        int nx = x + dx;
        int nz = z + dz;
        if (nx < 0) nx += Chunk.SizeX;
        if (nx >= Chunk.SizeX) nx -= Chunk.SizeX;
        if (nz < 0) nz += Chunk.SizeZ;
        if (nz >= Chunk.SizeZ) nz -= Chunk.SizeZ;

        var currentBlock = chunk.GetBlock(x, y, z);
        var currentDef = BlockRegistry.Get(currentBlock);
        if (currentDef.IsSolid && !currentDef.IsTransparent)
        {
            return;
        }

        byte neighborLight = neighbor.GetTorchLight(nx, y, nz);
        if (neighborLight <= 1)
        {
            return;
        }

        byte next = (byte)(neighborLight - 1);
        if (next <= chunk.GetTorchLight(x, y, z))
        {
            return;
        }

        chunk.SetTorchLight(x, y, z, next);
        queue.Enqueue((x, y, z));
    }
}
