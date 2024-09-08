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

    public override void UsePass(BackendPass pass, Action<BackendUsePassContext> action)
    {
        _device.BeginRenderPass((VulkanPass)pass, _commandBuffer, _device.SwapchainRenderTarget.Extent);
        action(new VulkanUsePassContext(_device, _commandBuffer));
        _device.EndRenderPass(_commandBuffer);
    }
}