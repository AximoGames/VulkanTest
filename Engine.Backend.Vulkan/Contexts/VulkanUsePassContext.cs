using OpenTK;
using OpenTK.Mathematics;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanUsePassContext : BackendUsePassContext
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

    /// <remarks>Consider using <see cref="VulkanDevice.ClearColor"/> instead</remarks>
    public override void Clear(Color3<Rgb> clearColor, Box2i rect)
    {
        VkClearAttachment clearAttachment = new VkClearAttachment
        {
            aspectMask = VkImageAspectFlags.Color,
            colorAttachment = 0,
            clearValue = new VkClearValue { color = clearColor.ToVkClearColorValue() },
        };

        VkClearRect clearRect = new VkClearRect
        {
            rect = rect.ToVkRect2D(),
            baseArrayLayer = 0,
            layerCount = 1,
        };

        vkCmdClearAttachments(_commandBuffer, 1, &clearAttachment, 1, &clearRect);
    }
}