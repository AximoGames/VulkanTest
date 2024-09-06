using OpenTK;
using OpenTK.Mathematics;

namespace Engine;

public class RenderContext
{
    public void BindVertexBuffer(Buffer buffer, uint binding = 0)
        => throw new NotImplementedException();

    public void BindIndexBuffer(Buffer buffer)
        => throw new NotImplementedException();

    /// <remarks>Consider using <see cref="Engine.Backend.Vulkan.Engine.Backend.Vulkan.VulkanGraphicsDevice.ClearColor"/></remarks>
    public void Clear(Color3<Rgb> clearColor)
        => throw new NotImplementedException();

    /// <remarks>Consider using <see cref="Engine.Backend.Vulkan.Engine.Backend.Vulkan.VulkanGraphicsDevice.ClearColor"/></remarks>
    public void Clear(Color3<Rgb> clearColor, Box2i rect)
        => throw new NotImplementedException();

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        => throw new NotImplementedException();
}