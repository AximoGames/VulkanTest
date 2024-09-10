using OpenTK;
using OpenTK.Mathematics;

namespace Engine;

public class RenderPipelineContext
{
    private readonly BackendRenderContext _backendContext;

    internal RenderPipelineContext(BackendRenderContext backendContext)
    {
        _backendContext = backendContext;
    }

    public void BindVertexBuffer(Buffer buffer, uint binding = 0)
        => _backendContext.BindVertexBuffer(buffer.BackendBuffer, binding);

    public void BindIndexBuffer(Buffer buffer)
        => _backendContext.BindIndexBuffer(buffer.BackendBuffer);

    public void Clear(Color3<Rgb> clearColor)
        => _backendContext.Clear(clearColor);

    public void Clear(Color3<Rgb> clearColor, Box2i rect)
        => _backendContext.Clear(clearColor, rect);

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        => _backendContext.DrawIndexed(indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

    public void BindUniformBuffer(Buffer buffer, uint set, uint binding)
        => _backendContext.BindUniformBuffer(buffer.BackendBuffer, set, binding);

    public void BindImage(Image image, Sampler sampler, uint set, uint binding)
        => _backendContext.BindImage(image.BackendImage, sampler.BackendSampler, set, binding);

    public void BindImage(RenderTarget image, Sampler sampler, uint set, uint binding)
        => _backendContext.BindImage(image.BackendRenderTarget.GetImage(_backendContext.FrameContext.CurrentSwapchainImageIndex), sampler.BackendSampler, set, binding);

    public void SetPushConstants<T>(ShaderStageFlags stageFlags, uint offset, T[] data) where T : unmanaged
        => _backendContext.SetPushConstants(stageFlags, offset, data);

    public void SetPushConstants<T>(ShaderStageFlags stageFlags, uint offset, T data) where T : unmanaged
        => _backendContext.SetPushConstants(stageFlags, offset, data);
}