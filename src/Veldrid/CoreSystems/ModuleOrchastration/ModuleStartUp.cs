using Microsoft.Extensions.DependencyInjection;
using Veldrid.CoreSystems.Logging;
using Veldrid.GameplayFoundations.EventSystem;
using Veldrid.HumanInterfaceDevice.PhysicalDeviceIO;
using Veldrid.PlatformIndependence.Windowing;

namespace Veldrid.CoreSystems.ModuleOrchastration;

internal static class ModuleStartUp
{
    public static IServiceCollection AddEngineServices(this IServiceCollection services, WindowCreateInfo windowCreateInfo)
    {
        // Platform Independence
        services.AddWindowing(windowCreateInfo);

        // Core Systems
        services.AddEngineLogging();

        // Gameplay Foundations
        services.AddSingleton<IEventSystem, EventSystem>();

        // Low-Level Renderer

        // Human Interface Device
        services.AddSingleton<IPhysicalDeviceIO, PhysicalDeviceIO>();

        return services;
    }
}
