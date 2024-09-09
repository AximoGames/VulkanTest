using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal class VulkanBuffer : BackendBuffer
{
    private readonly VulkanDevice _device;
    internal VkBuffer Buffer { get; }
    internal VkDeviceMemory Memory { get; }

    internal VulkanBuffer(Type elementType, uint size, VulkanDevice device, VkBuffer buffer, VkDeviceMemory memory)
        : base(elementType, size)
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