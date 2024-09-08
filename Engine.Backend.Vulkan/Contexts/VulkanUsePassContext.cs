using Vortice.Vulkan;

namespace Engine.Vulkan;

internal class VulkanUsePassContext : BackendUsePassContext
{
    private VkCommandBuffer _commandBuffer;
    private VulkanDevice _device;

    internal VulkanUsePassContext(VulkanDevice device, VkCommandBuffer commandBuffer)
    {
        _commandBuffer = commandBuffer;
        _device = device;
    }

    public override void UsePipeline(BackendPipeline pipeline, Action<BackendRenderContext> action)
    {
        _device.BindPipeline(_commandBuffer, ((VulkanPipeline)pipeline).PipelineHandle);
        action(new VulkanRenderContext(_device, _commandBuffer, _device.SwapchainRenderTarget.Extent));
    }
}