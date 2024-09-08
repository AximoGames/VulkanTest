using Vortice.Vulkan;

namespace Engine.Vulkan;

internal class VulkanRenderFrameContext : BackendRenderFrameContext
{
    private VkCommandBuffer _commandBuffer;
    private VulkanDevice _device;

    internal VulkanRenderFrameContext(VulkanDevice device, VkCommandBuffer commandBuffer)
    {
        _commandBuffer = commandBuffer;
        _device = device;
    }

    public override void UsePass(Action<BackendUsePassContext> action)
    {
        _device.BeginRenderPass(_commandBuffer, _device.Swapchain.Extent);
        action(new VulkanUsePassContext(_device, _commandBuffer));
        _device.EndRenderPass(_commandBuffer);
    }
}