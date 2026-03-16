using System;

namespace MiniCRUFT.Core;

public sealed class NoiseService
{
    private readonly PerlinNoise _perlin;

    public NoiseService(int seed)
    {
        _perlin = new PerlinNoise(seed);
    }

    public float Continental(float x, float z) => _perlin.Fractal2D(x, z, 0.0018f, 4, 0.5f, 2.0f);
    public float Peaks(float x, float z) => _perlin.Fractal2D(x, z, 0.0065f, 4, 0.5f, 2.0f);
    public float Erosion(float x, float z) => _perlin.Fractal2D(x, z, 0.0035f, 4, 0.5f, 2.0f);
    public float Biome(float x, float z) => _perlin.Fractal2D(x, z, 0.0015f, 3, 0.5f, 2.0f);
    public float River(float x, float z) => _perlin.Fractal2D(x, z, 0.004f, 3, 0.5f, 2.0f);
    public float Cave(float x, float y, float z) => _perlin.Fractal3D(x, y, z, 0.02f, 3, 0.55f, 2.1f);

    private sealed class PerlinNoise
    {
        private readonly int[] _perm = new int[512];

        public PerlinNoise(int seed)
        {
            var rand = new Random(seed);
            int[] p = new int[256];
            for (int i = 0; i < 256; i++)
            {
                p[i] = i;
            }

            for (int i = 255; i > 0; i--)
            {
                int swap = rand.Next(i + 1);
                (p[i], p[swap]) = (p[swap], p[i]);
            }

            for (int i = 0; i < 512; i++)
            {
                _perm[i] = p[i & 255];
            }
        }

        public float Fractal2D(float x, float y, float frequency, int octaves, float gain, float lacunarity)
        {
            float sum = 0;
            float amp = 1f;
            float max = 0;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise2D(x * frequency, y * frequency) * amp;
                max += amp;
                amp *= gain;
                frequency *= lacunarity;
            }
            return sum / max;
        }

        public float Fractal3D(float x, float y, float z, float frequency, int octaves, float gain, float lacunarity)
        {
            float sum = 0;
            float amp = 1f;
            float max = 0;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise3D(x * frequency, y * frequency, z * frequency) * amp;
                max += amp;
                amp *= gain;
                frequency *= lacunarity;
            }
            return sum / max;
        }

        private float Noise2D(float x, float y)
        {
            int xi = (int)MathF.Floor(x) & 255;
            int yi = (int)MathF.Floor(y) & 255;
            float xf = x - MathF.Floor(x);
            float yf = y - MathF.Floor(y);

            float u = Fade(xf);
            float v = Fade(yf);

            int aa = _perm[_perm[xi] + yi];
            int ab = _perm[_perm[xi] + yi + 1];
            int ba = _perm[_perm[xi + 1] + yi];
            int bb = _perm[_perm[xi + 1] + yi + 1];

            float x1 = Lerp(Grad2D(aa, xf, yf), Grad2D(ba, xf - 1, yf), u);
            float x2 = Lerp(Grad2D(ab, xf, yf - 1), Grad2D(bb, xf - 1, yf - 1), u);

            return Lerp(x1, x2, v);
        }

        private float Noise3D(float x, float y, float z)
        {
            int xi = (int)MathF.Floor(x) & 255;
            int yi = (int)MathF.Floor(y) & 255;
            int zi = (int)MathF.Floor(z) & 255;
            float xf = x - MathF.Floor(x);
            float yf = y - MathF.Floor(y);
            float zf = z - MathF.Floor(z);

            float u = Fade(xf);
            float v = Fade(yf);
            float w = Fade(zf);

            int aaa = _perm[_perm[_perm[xi] + yi] + zi];
            int aba = _perm[_perm[_perm[xi] + yi + 1] + zi];
            int aab = _perm[_perm[_perm[xi] + yi] + zi + 1];
            int abb = _perm[_perm[_perm[xi] + yi + 1] + zi + 1];
            int baa = _perm[_perm[_perm[xi + 1] + yi] + zi];
            int bba = _perm[_perm[_perm[xi + 1] + yi + 1] + zi];
            int bab = _perm[_perm[_perm[xi + 1] + yi] + zi + 1];
            int bbb = _perm[_perm[_perm[xi + 1] + yi + 1] + zi + 1];

            float x1 = Lerp(Grad3D(aaa, xf, yf, zf), Grad3D(baa, xf - 1, yf, zf), u);
            float x2 = Lerp(Grad3D(aba, xf, yf - 1, zf), Grad3D(bba, xf - 1, yf - 1, zf), u);
            float y1 = Lerp(x1, x2, v);

            float x3 = Lerp(Grad3D(aab, xf, yf, zf - 1), Grad3D(bab, xf - 1, yf, zf - 1), u);
            float x4 = Lerp(Grad3D(abb, xf, yf - 1, zf - 1), Grad3D(bbb, xf - 1, yf - 1, zf - 1), u);
            float y2 = Lerp(x3, x4, v);

            return Lerp(y1, y2, w);
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        private static float Grad2D(int hash, float x, float y)
        {
            int h = hash & 3;
            float u = h < 2 ? x : y;
            float v = h < 2 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }

        private static float Grad3D(int hash, float x, float y, float z)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : h == 12 || h == 14 ? x : z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
