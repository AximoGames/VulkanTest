using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine;

public class VulkanBuffer
{
    private readonly VulkanDevice _device;
    internal VkBuffer Buffer { get; }
    internal  VkDeviceMemory Memory { get; }

    internal VulkanBuffer(VulkanDevice device, VkBuffer buffer, VkDeviceMemory memory)
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