using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VulkanTest;

public unsafe class RenderContext
{
    private readonly VulkanDevice _device;
    private readonly BufferManager _bufferManager;
    private readonly VkCommandBuffer _commandBuffer;
    private readonly VkExtent2D _extent;

    public RenderContext(VulkanDevice device, BufferManager bufferManager, VkCommandBuffer commandBuffer, VkExtent2D extent)
    {
        _device = device;
        _bufferManager = bufferManager;
        _commandBuffer = commandBuffer;
        _extent = extent;
    }

    public void Draw()
    {
        vkCmdBindVertexBuffer(_commandBuffer, 0, _bufferManager.VertexBuffer);
        vkCmdBindIndexBuffer(_commandBuffer, _bufferManager.IndexBuffer, 0, VkIndexType.Uint16);

        vkCmdDrawIndexed(_commandBuffer, 6, 1, 0, 0, 0);
    }
}