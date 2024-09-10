using Vortice.Vulkan;

namespace Engine.Vulkan;

internal class VulkanRenderFrameContext : BackendRenderFrameContext
{
    private VkCommandBuffer _commandBuffer;
    private VulkanDevice _device;
    private uint _currentSwapchainImageIndex;

    internal VulkanRenderFrameContext(VulkanDevice device, VkCommandBuffer commandBuffer, uint currentSwapchainImageIndex)
    {
        _commandBuffer = commandBuffer;
        _device = device;
        _currentSwapchainImageIndex = currentSwapchainImageIndex;
    }

    public override uint CurrentSwapchainImageIndex => _currentSwapchainImageIndex;

    public override void UsePass(BackendPass pass, Action<BackendUsePassContext> action)
    {
        _device.BeginRenderPass((VulkanPass)pass, _commandBuffer, _device.SwapchainRenderTarget.Extent);
        action(new VulkanUsePassContext(this, _device, _commandBuffer));
        _device.EndRenderPass(_commandBuffer);
    }

    public override BackendDevice Device => _device;
}