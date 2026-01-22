using Silk.NET.Core.Contexts;
using Silk.NET.Windowing;
using System;
using Veldrid.LowLevelRenderer.Core;

namespace Veldrid.PlatformIndependence.Windowing;

internal static class IWindowExtensions
{
    public static unsafe SwapchainSource ToSwapchainSource(this IWindow window)
    {
        if (window?.Native == null)
        {
            throw new InvalidOperationException("Window has no native handle available.");
        }

        INativeWindow native = window.Native;

        if (native.Win32.HasValue)
        {
            return SwapchainSource.CreateWin32(
                native.Win32.Value.Hwnd,
                native.Win32.Value.HInstance);
        }

        if (native.X11.HasValue)
        {
            return SwapchainSource.CreateXlib(
                native.X11.Value.Display,
                (nint)native.X11.Value.Window);
        }

        if (native.Wayland.HasValue)
        {
            return SwapchainSource.CreateWayland(
                native.Wayland.Value.Display,
                native.Wayland.Value.Surface);
        }

        if (native.Cocoa.HasValue)
        {
            return SwapchainSource.CreateNSWindow(native.Cocoa.Value);
        }

        throw new PlatformNotSupportedException($"Unsupported window platform: {native.Kind}");
    }
}
