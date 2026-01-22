using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using System;
using Veldrid.GameplayFoundations.EventSystem;
using Window = Silk.NET.Windowing.Window;

namespace Veldrid.PlatformIndependence.Windowing;

/// <summary>
/// Provides dependency injection configuration for windowing services in the rendering engine.
/// </summary>
internal static class WindowingService
{
    internal const uint VK_VERSION_MAJOR = 1;
    internal const uint VK_VERSION_MINOR = 3;

    /// <summary>
    /// Adds windowing services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="windowCreateInfo">The engine configuration containing window settings such as title, width, and height.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddWindowing(this IServiceCollection services, WindowCreateInfo windowCreateInfo)
    {
        services.AddSingleton<IWindow>(sp =>
        {
            return CreateVulkanWindow(sp.GetRequiredService<IEventSystem>(), windowCreateInfo);
        });

        return services;
    }

    private static IWindow CreateVulkanWindow(IEventSystem eventSystem, WindowCreateInfo windowCreateInfo)
    {
        WindowOptions options = WindowOptions.DefaultVulkan with
        {
            Title = windowCreateInfo.WindowTitle,
            Size = new Vector2D<int>(windowCreateInfo.WindowWidth, windowCreateInfo.WindowHeight),
            WindowState = windowCreateInfo.WindowInitialState,
            API = GraphicsAPI.DefaultVulkan with
            {
                Version = new APIVersion(Convert.ToInt32(VK_VERSION_MAJOR), Convert.ToInt32(VK_VERSION_MINOR))
            }
        };

        IWindow window = Window.Create(options);
        window.Initialize();

        if (window.VkSurface is null)
        {
            throw new PlatformNotSupportedException("Windowing platform doesn't support Vulkan.");
        }

        window.Center();

        HookWindowEvents(window, eventSystem);

        return window;
    }

    private static void HookWindowEvents(IWindow window, IEventSystem eventSystem)
    {
        window.Load += () => eventSystem.Publish(window, new WindowLoadEventArgs());
        window.Update += (delta) => eventSystem.Publish(window, new WindowUpdateEventArgs(delta));
        window.Render += (delta) => eventSystem.Publish(window, new WindowRenderEventArgs(delta));
        window.Resize += (size) => eventSystem.Publish(window, new WindowResizeEventArgs(size.X, size.Y));
        window.FramebufferResize += (size) => eventSystem.Publish(window, new WindowFramebufferResizeEventArgs(size.X, size.Y));
        window.FocusChanged += (focused) => eventSystem.Publish(window, new WindowFocusChangedEventArgs(focused));
        window.Closing += () => eventSystem.Publish(window, new WindowCloseEventArgs());
    }
}
