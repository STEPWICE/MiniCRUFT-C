using System;
using System.IO;
using System.Runtime.InteropServices;
using Veldrid.Sdl2;

namespace MiniCRUFT.Renderer;

internal static class WindowIcon
{
    private const int WmSetIcon = 0x0080;
    private const int IconSmall = 0;
    private const int IconBig = 1;
    private const int ImageIcon = 1;
    private const uint LoadFromFile = 0x0010;
    private const uint Shared = 0x8000;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadImage(IntPtr hInst, string name, int type, int cx, int cy, uint fuLoad);

    public static void TryApply(Sdl2Window window, string iconPath)
    {
        if (!OperatingSystem.IsWindows() || window.Handle == IntPtr.Zero || !File.Exists(iconPath))
        {
            return;
        }

        IntPtr smallIcon = LoadImage(IntPtr.Zero, iconPath, ImageIcon, 16, 16, LoadFromFile | Shared);
        IntPtr bigIcon = LoadImage(IntPtr.Zero, iconPath, ImageIcon, 32, 32, LoadFromFile | Shared);

        if (smallIcon != IntPtr.Zero)
        {
            SendMessage(window.Handle, WmSetIcon, (IntPtr)IconSmall, smallIcon);
        }

        if (bigIcon != IntPtr.Zero)
        {
            SendMessage(window.Handle, WmSetIcon, (IntPtr)IconBig, bigIcon);
        }
    }
}
