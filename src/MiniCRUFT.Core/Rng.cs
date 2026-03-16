using System;

namespace MiniCRUFT.Core;

public sealed class Rng
{
    private readonly Random _random;

    public Rng(int seed)
    {
        _random = new Random(seed);
    }

    public int Next(int min, int max) => _random.Next(min, max);
    public float NextFloat() => (float)_random.NextDouble();
}
