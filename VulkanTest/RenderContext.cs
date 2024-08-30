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

    public void BindVertexBuffer(uint binding = 0)
        => vkCmdBindVertexBuffer(_commandBuffer, binding, _bufferManager.VertexBuffer);

    public void BindIndexBuffer(VkIndexType indexType = VkIndexType.Uint16)
        => vkCmdBindIndexBuffer(_commandBuffer, _bufferManager.IndexBuffer, 0, indexType);

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        => vkCmdDrawIndexed(_commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
}