using Microsoft.Extensions.DependencyInjection;
using Silk.NET.Windowing;
using System;
using Veldrid.CoreSystems.ModuleOrchastration;
using Veldrid.LowLevelRenderer.Core;
using Veldrid.PlatformIndependence.Windowing;

namespace Veldrid;

public static class VeldridStartup
{
    public static void CreateWindowAndGraphicsDevice(WindowCreateInfo windowCI, GraphicsDeviceOptions deviceOptions, out IWindow window, out GraphicsDevice gd)
    {
        IServiceCollection services = new ServiceCollection().AddEngineServices(windowCI);
        IServiceProvider provider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(provider);

        window = Ioc.Default.GetRequiredService<IWindow>();
        gd = CreateGraphicsDevice(window, deviceOptions);
    }

    public static GraphicsDevice CreateGraphicsDevice(IWindow window, GraphicsDeviceOptions deviceOptions)
    {
        SwapchainDescription scDesc = new(
            window.ToSwapchainSource(),
            (uint)window.Size.X,
            (uint)window.Size.Y,
            deviceOptions.SwapchainDepthFormat,
            deviceOptions.SyncToVerticalBlank,
            deviceOptions.SwapchainSrgbFormat);

        return GraphicsDevice.CreateVulkan(deviceOptions, scDesc);
    }
}
