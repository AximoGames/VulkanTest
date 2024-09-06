using OpenTK;
using OpenTK.Mathematics;
using Vortice.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanBackendRenderContext : BackendRenderContext
{
    private readonly VulkanDevice _device;
    private readonly VkCommandBuffer _commandBuffer;
    private readonly Vector2i _extent;

    internal VulkanBackendRenderContext(VulkanDevice device, VkCommandBuffer commandBuffer, Vector2i extent)
    {
        _device = device;
        _commandBuffer = commandBuffer;
        _extent = extent;
    }

    public override void BindVertexBuffer(BackendBuffer backendBuffer, uint binding = 0)
        => Vortice.Vulkan.Vulkan.vkCmdBindVertexBuffer(_commandBuffer, binding, ((VulkanBackendBuffer)backendBuffer).Buffer);

    public override void BindIndexBuffer(BackendBuffer backendBuffer)
        => Vortice.Vulkan.Vulkan.vkCmdBindIndexBuffer(_commandBuffer, ((VulkanBackendBuffer)backendBuffer).Buffer, 0, backendBuffer.ElementType.ToVkIndexType());

    public override void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        => Vortice.Vulkan.Vulkan.vkCmdDrawIndexed(_commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

    /// <remarks>Consider using <see cref="VulkanDevice.ClearColor"/> instead</remarks>
    public override void Clear(Color3<Rgb> clearColor)
    {
        Clear(clearColor, new Box2i(Vector2i.Zero, _extent));
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

        Vortice.Vulkan.Vulkan.vkCmdClearAttachments(_commandBuffer, 1, &clearAttachment, 1, &clearRect);
    }
}