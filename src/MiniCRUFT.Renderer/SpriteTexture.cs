using System;
using System.Numerics;
using MiniCRUFT.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;

namespace MiniCRUFT.Renderer;

public sealed class SpriteTexture : IDisposable
{
    public Texture Texture { get; }
    public Sampler Sampler { get; }
    public Vector2 Size { get; }

    private SpriteTexture(Texture texture, Sampler sampler, Vector2 size)
    {
        Texture = texture;
        Sampler = sampler;
        Size = size;
    }

    public static SpriteTexture Load(GraphicsDevice device, AssetStore assets, string relativePath, bool repeat)
    {
        using var stream = assets.OpenStream(relativePath);
        using var image = Image.Load<Rgba32>(stream);

        var pixelData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixelData);

        var texture = device.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
            (uint)image.Width, (uint)image.Height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, TextureUsage.Sampled));
        device.UpdateTexture(texture, pixelData, 0, 0, 0, (uint)image.Width, (uint)image.Height, 1u, 0u, 0u);

        var sampler = device.ResourceFactory.CreateSampler(new SamplerDescription
        {
            AddressModeU = repeat ? SamplerAddressMode.Wrap : SamplerAddressMode.Clamp,
            AddressModeV = repeat ? SamplerAddressMode.Wrap : SamplerAddressMode.Clamp,
            AddressModeW = SamplerAddressMode.Clamp,
            Filter = SamplerFilter.MinPoint_MagPoint_MipPoint,
            MaximumAnisotropy = 1
        });

        return new SpriteTexture(texture, sampler, new Vector2(image.Width, image.Height));
    }

    public void Dispose()
    {
        Texture.Dispose();
        Sampler.Dispose();
    }
}
