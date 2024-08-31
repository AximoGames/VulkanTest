using OpenTK;
using OpenTK.Mathematics;

namespace Engine.Vulkan;

public abstract class RenderContext
{
    public abstract void BindVertexBuffer(Buffer buffer, uint binding = 0);
    public abstract void BindIndexBuffer(Buffer buffer);

    /// <remarks>Consider using <see cref="Engine.Vulkan.Engine.Vulkan.VulkanGraphicsDevice.ClearColoremarks>
    public abstract void Clear(Color3<Rgb> clearColor);

    /// <remarks>Consider using <see cref="Engine.Vulkan.Engine.Vulkan.VulkanGraphicsDevice.ClearColoremarks>
    public abstract void Clear(Color3<Rgb> clearColor, Box2i rect);

    public abstract void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0);
}