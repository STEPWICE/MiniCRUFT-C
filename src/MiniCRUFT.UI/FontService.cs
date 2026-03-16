using System.IO;
using FontStashSharp;

namespace MiniCRUFT.UI;

public sealed class FontService
{
    public FontSystem FontSystem { get; }

    public FontService(string ttfPath)
    {
        FontSystem = new FontSystem(new FontSystemSettings
        {
            FontResolutionFactor = 2f,
            KernelWidth = 2,
            KernelHeight = 2
        });

        if (File.Exists(ttfPath))
        {
            FontSystem.AddFont(File.ReadAllBytes(ttfPath));
        }
    }
}
