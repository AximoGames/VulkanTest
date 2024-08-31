using Vortice.Vulkan;

namespace VulkanTest;

public class ShaderModule
{
    private readonly VulkanDevice _device;
    internal VkShaderModule Module { get; }

    internal ShaderModule(VulkanDevice device, VkShaderModule module)
    {
        _device = device;
        Module = module;
    }

    public unsafe void Free()
    {
        Vulkan.vkDestroyShaderModule(_device.LogicalDevice, Module, null);
    }
}