using System;
using MiniCRUFT.Core;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace MiniCRUFT.Renderer;

public sealed class RenderDevice : IDisposable
{
    public Sdl2Window Window { get; }
    public GraphicsDevice GraphicsDevice { get; }
    public CommandList CommandList { get; }

    private bool _resized;

    public RenderDevice(Sdl2Window window, GraphicsDevice graphicsDevice)
    {
        Window = window;
        GraphicsDevice = graphicsDevice;
        CommandList = graphicsDevice.ResourceFactory.CreateCommandList();
        Window.Resized += () => _resized = true;
    }

    public static RenderDevice Create(string title, int width, int height, bool vsync)
    {
        try
        {
            var windowCI = new WindowCreateInfo(100, 100, width, height, WindowState.Normal, title);
            var options = new GraphicsDeviceOptions(
                debug: false,
                swapchainDepthFormat: PixelFormat.D24_UNorm_S8_UInt,
                syncToVerticalBlank: vsync,
                resourceBindingModel: ResourceBindingModel.Improved,
                preferDepthRangeZeroToOne: true,
                preferStandardClipSpaceYDirection: true);

            var window = VeldridStartup.CreateWindow(ref windowCI);
            GraphicsDevice gd;
            try
            {
                gd = VeldridStartup.CreateGraphicsDevice(window, options, GraphicsBackend.Direct3D11);
                Log.Info("Graphics backend: Direct3D11");
            }
            catch (Exception ex)
            {
                Log.Warn($"Direct3D11 init failed: {ex.Message}");
                try
                {
                    gd = VeldridStartup.CreateGraphicsDevice(window, options, GraphicsBackend.Vulkan);
                    Log.Info("Graphics backend: Vulkan");
                }
                catch (Exception ex2)
                {
                    Log.Warn($"Vulkan init failed: {ex2.Message}");
                    gd = VeldridStartup.CreateGraphicsDevice(window, options, GraphicsBackend.OpenGL);
                    Log.Info("Graphics backend: OpenGL");
                }
            }

            return new RenderDevice(window, gd);
        }
        catch (Exception ex)
        {
            Log.Error($"RenderDevice init failed: {ex}");
            throw;
        }
    }

    public void ResizeIfNeeded()
    {
        if (_resized)
        {
            _resized = false;
            GraphicsDevice.MainSwapchain.Resize((uint)Window.Width, (uint)Window.Height);
        }
    }

    public void Dispose()
    {
        CommandList.Dispose();
        GraphicsDevice.Dispose();
    }
}
