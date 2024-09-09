using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanSampler : BackendSampler
{
    private readonly VulkanDevice _device;
    internal VkSampler Sampler { get; }

    public VulkanSampler(VulkanDevice device, VkSampler sampler)
    {
        _device = device;
        Sampler = sampler;
    }

    public override void Dispose()
    {
        vkDestroySampler(_device.LogicalDevice, Sampler, null);
    }
}