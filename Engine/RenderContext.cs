using Vortice.Vulkan;

namespace Engine.Vulkan;

public abstract class RenderContext
{
    public abstract void BindVertexBuffer(Buffer buffer, uint binding = 0);
    public abstract void BindIndexBuffer(Buffer buffer, VkIndexType indexType = VkIndexType.Uint16);

    /// <remarks>Consider using <see cref="Engine.Vulkan.Engine.Vulkan.VulkanGraphicsDevice.ClearColoremarks>
    public abstract void Clear(VkClearColorValue clearColor);

    /// <remarks>Consider using <see cref="Engine.Vulkan.Engine.Vulkan.VulkanGraphicsDevice.ClearColoremarks>
    public abstract unsafe void Clear(VkClearColorValue clearColor, VkRect2D rect);

    public abstract void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0);
}