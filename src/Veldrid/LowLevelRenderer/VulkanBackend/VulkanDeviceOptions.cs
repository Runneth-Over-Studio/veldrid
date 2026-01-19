namespace Veldrid.LowLevelRenderer.VulkanBackend;

/// <summary>
/// A structure describing Vulkan-specific device creation options.
/// </summary>
/// <remarks>
/// Constructs a new VulkanDeviceOptions.
/// </remarks>
/// <param name="instanceExtensions">An array of required Vulkan instance extensions. Entries in this array will be
/// enabled in the GraphicsDevice's created VkInstance.</param>
/// <param name="deviceExtensions">An array of required Vulkan device extensions. Entries in this array will be enabled
/// in the GraphicsDevice's created VkDevice.</param>
public struct VulkanDeviceOptions(string[] instanceExtensions, string[] deviceExtensions)
{
    /// <summary>
    /// An array of required Vulkan instance extensions. Entries in this array will be enabled in the GraphicsDevice's
    /// created VkInstance.
    /// </summary>
    public string[] InstanceExtensions = instanceExtensions;

    /// <summary>
    /// An array of required Vulkan device extensions. Entries in this array will be enabled in the GraphicsDevice's
    /// created VkDevice.
    /// </summary>
    public string[] DeviceExtensions = deviceExtensions;
}
