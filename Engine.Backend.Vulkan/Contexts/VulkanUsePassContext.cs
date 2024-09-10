using Vortice.Vulkan;

namespace Engine.Vulkan;

internal class VulkanUsePassContext : BackendUsePassContext
{
    private VkCommandBuffer _commandBuffer;
    private VulkanDevice _device;

    internal VulkanUsePassContext(BackendRenderFrameContext frameContext, VulkanDevice device, VkCommandBuffer commandBuffer)
    {
        _commandBuffer = commandBuffer;
        FrameContext = frameContext;
        _device = device;
    }

    public override BackendRenderFrameContext FrameContext { get; }

    public override void UsePipeline(BackendPipeline pipeline, Action<BackendRenderContext> action)
    {
        _device.BindPipeline(_commandBuffer, ((VulkanPipeline)pipeline).Pipeline);
        action(new VulkanRenderPipelineContext(this, _device, _commandBuffer, _device.SwapchainRenderTarget.Extent, (VulkanPipeline)pipeline));
    }
}