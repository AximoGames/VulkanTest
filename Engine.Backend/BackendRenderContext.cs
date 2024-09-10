using OpenTK;
using OpenTK.Mathematics;

namespace Engine;

public abstract class BackendRenderContext
{
    public abstract void BindVertexBuffer(BackendBuffer backendBuffer, uint binding = 0);
    public abstract void BindIndexBuffer(BackendBuffer backendBuffer);

    /// <remarks>Consider using <see cref="Engine.Backend.Vulkan.Engine.Backend.Vulkan.VulkanGraphicsDevice.ClearColor"/></remarks>
    public abstract void Clear(Color3<Rgb> clearColor);

    /// <remarks>Consider using <see cref="Engine.Backend.Vulkan.Engine.Backend.Vulkan.VulkanGraphicsDevice.ClearColor"/></remarks>
    public abstract void Clear(Color3<Rgb> clearColor, Box2i rect);

    public abstract void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0);
    public abstract void BindUniformBuffer(BackendBuffer buffer, uint set, uint binding);
    public abstract void SetPushConstants<T>(ShaderStageFlags stageFlags, uint offset, T[] data) where T : unmanaged;
    public abstract void SetPushConstants<T>(ShaderStageFlags stageFlags, uint offset, T data) where T : unmanaged;
    public abstract void BindImage(BackendImage image, BackendSampler sampler, uint set, uint binding);
}