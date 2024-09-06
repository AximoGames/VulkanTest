using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal class VulkanBackendBuffer : BackendBuffer
{
    private readonly VulkanDevice _device;
    internal VkBuffer Buffer { get; }
    internal VkDeviceMemory Memory { get; }

    internal VulkanBackendBuffer(Type elementType, VulkanDevice device, VkBuffer buffer, VkDeviceMemory memory)
        : base(elementType)
    {
        _device = device;
        Buffer = buffer;
        Memory = memory;
    }

    public unsafe void Dispose()
    {
        vkDestroyBuffer(_device.LogicalDevice, Buffer, null);
        vkFreeMemory(_device.LogicalDevice, Memory, null);
    }
}