using OpenTK;
using Vortice.Vulkan;

namespace Engine.Vulkan;

public unsafe class VulkanRenderContext : RenderContext
{
    private readonly VulkanDevice _device;
    private readonly VkCommandBuffer _commandBuffer;
    private readonly VkExtent2D _extent;

    internal VulkanRenderContext(VulkanDevice device, VkCommandBuffer commandBuffer, VkExtent2D extent)
    {
        _device = device;
        _commandBuffer = commandBuffer;
        _extent = extent;
    }

    public override void BindVertexBuffer(Buffer buffer, uint binding = 0)
        => Vortice.Vulkan.Vulkan.vkCmdBindVertexBuffer(_commandBuffer, binding, ((VulkanBuffer)buffer).Buffer);

    public override void BindIndexBuffer(Buffer buffer)
        => Vortice.Vulkan.Vulkan.vkCmdBindIndexBuffer(_commandBuffer, ((VulkanBuffer)buffer).Buffer, 0, buffer.ElementType.ToVkIndexType());

    public override void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        => Vortice.Vulkan.Vulkan.vkCmdDrawIndexed(_commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

    /// <remarks>Consider using <see cref="VulkanGraphicsDevice.ClearColor"/> instead</remarks>
    public override void Clear(Color3<Rgb> clearColor)
    {
        Clear(clearColor, new VkRect2D { extent = _extent });
    }

    /// <remarks>Consider using <see cref="VulkanGraphicsDevice.ClearColor"/> instead</remarks>
    public override void Clear(Color3<Rgb> clearColor, VkRect2D rect)
    {
        VkClearAttachment clearAttachment = new VkClearAttachment
        {
            aspectMask = VkImageAspectFlags.Color,
            colorAttachment = 0,
            clearValue = new VkClearValue { color = clearColor.ToVkClearColorValue() },
        };

        VkClearRect clearRect = new VkClearRect
        {
            rect = rect,
            baseArrayLayer = 0,
            layerCount = 1,
        };

        Vortice.Vulkan.Vulkan.vkCmdClearAttachments(_commandBuffer, 1, &clearAttachment, 1, &clearRect);
    }
}