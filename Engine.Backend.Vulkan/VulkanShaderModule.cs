using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal class VulkanShaderModule
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
        vkDestroyShaderModule(_device.LogicalDevice, Module, null);
    }
}