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

    public static WindowState GetInitialWindowState(bool fullscreen)
    {
        return fullscreen ? WindowState.BorderlessFullScreen : WindowState.Normal;
    }

    public static (int X, int Y) GetCenteredWindowPosition(int displayX, int displayY, int displayWidth, int displayHeight, int windowWidth, int windowHeight)
    {
        int width = Math.Max(1, windowWidth);
        int height = Math.Max(1, windowHeight);
        int centeredX = displayX + Math.Max(0, (displayWidth - width) / 2);
        int centeredY = displayY + Math.Max(0, (displayHeight - height) / 2);
        return (centeredX, centeredY);
    }

    public unsafe void CenterOnCurrentDisplayIfNeeded()
    {
        if (Window.WindowState == WindowState.BorderlessFullScreen || Window.WindowState == WindowState.FullScreen ||
            Window.WindowState == WindowState.Minimized || Window.WindowState == WindowState.Hidden)
        {
            return;
        }

        int displayIndex = Sdl2Native.SDL_GetWindowDisplayIndex(Window.SdlWindowHandle);
        if (displayIndex < 0)
        {
            return;
        }

        Rectangle displayBounds;
        if (Sdl2Native.SDL_GetDisplayBounds(displayIndex, &displayBounds) != 0)
        {
            return;
        }

        var (x, y) = GetCenteredWindowPosition(displayBounds.X, displayBounds.Y, displayBounds.Width, displayBounds.Height, Window.Width, Window.Height);
        Sdl2Native.SDL_SetWindowPosition(Window.SdlWindowHandle, x, y);
    }

    public static RenderDevice Create(string title, int x, int y, int width, int height, bool vsync, bool fullscreen, bool centerWindowOnStart)
    {
        try
        {
            var windowCI = new WindowCreateInfo(x, y, width, height, GetInitialWindowState(fullscreen), title);
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

            var renderDevice = new RenderDevice(window, gd);
            WindowIcon.TryApply(window, Path.Combine(AppContext.BaseDirectory, "Icon.ico"));
            if (centerWindowOnStart)
            {
                renderDevice.CenterOnCurrentDisplayIfNeeded();
            }

            return renderDevice;
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
            int width = Math.Max(1, Window.Width);
            int height = Math.Max(1, Window.Height);
            GraphicsDevice.MainSwapchain.Resize((uint)width, (uint)height);
        }
    }

    public void Dispose()
    {
        CommandList.Dispose();
        GraphicsDevice.Dispose();
    }
}
