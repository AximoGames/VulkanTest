using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine;

public unsafe class RenderContext
{
    private readonly VulkanDevice _device;
    private readonly VkCommandBuffer _commandBuffer;
    private readonly VkExtent2D _extent;

    internal RenderContext(VulkanDevice device, VkCommandBuffer commandBuffer, VkExtent2D extent)
    {
        _device = device;
        _commandBuffer = commandBuffer;
        _extent = extent;
    }

    public void BindVertexBuffer(VulkanBuffer buffer, uint binding = 0)
        => vkCmdBindVertexBuffer(_commandBuffer, binding, buffer.Buffer);

    public void BindIndexBuffer(VulkanBuffer buffer, VkIndexType indexType = VkIndexType.Uint16)
        => vkCmdBindIndexBuffer(_commandBuffer, buffer.Buffer, 0, indexType);

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        => vkCmdDrawIndexed(_commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

    /// <remarks>Consider using <see cref="GraphicsDevice.ClearColor"/> instead</remarks>
    public void Clear(VkClearColorValue clearColor)
    {
        Clear(clearColor, new VkRect2D { extent = _extent });
    }

    /// <remarks>Consider using <see cref="GraphicsDevice.ClearColor"/> instead</remarks>
    public void Clear(VkClearColorValue clearColor, VkRect2D rect)
    {
        VkClearAttachment clearAttachment = new VkClearAttachment
        {
            aspectMask = VkImageAspectFlags.Color,
            colorAttachment = 0,
            clearValue = new VkClearValue { color = clearColor }
        };

        VkClearRect clearRect = new VkClearRect
        {
            rect = rect,
            baseArrayLayer = 0,
            layerCount = 1
        };

        vkCmdClearAttachments(_commandBuffer, 1, &clearAttachment, 1, &clearRect);
    }
}