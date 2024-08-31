using Vortice.Vulkan;

namespace Engine;

public class VulkanShaderModule
{
    private readonly VulkanDevice _device;
    internal VkShaderModule Module { get; }

    internal VulkanShaderModule(VulkanDevice device, VkShaderModule module)
    {
        _device = device;
        Module = module;
    }

    public unsafe void Free()
    {
        Vulkan.vkDestroyShaderModule(_device.LogicalDevice, Module, null);
    }
}