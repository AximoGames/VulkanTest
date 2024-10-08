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

    public void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        => _backendContext.DrawIndexed(indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

    public void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
        => _backendContext.Draw(vertexCount, instanceCount, firstVertex, firstInstance);

    public void BindUniformBuffer(Buffer buffer, uint set, uint binding, Span<uint> dynamicOffsets)
        => _backendContext.BindUniformBuffer(buffer.BackendBuffer, set, binding, dynamicOffsets);

    public void BindUniformBuffer(Buffer buffer, uint set, uint binding)
        => _backendContext.BindUniformBuffer(buffer.BackendBuffer, set, binding, Span<uint>.Empty);

    public void BindImage(Image image, Sampler sampler, uint set, uint binding, Span<uint> dynamicOffsets)
        => _backendContext.BindImage(image.BackendImage, sampler.BackendSampler, set, binding, dynamicOffsets);

    public void BindImage(Image image, Sampler sampler, uint set, uint binding)
        => BindImage(image, sampler, set, binding, Span<uint>.Empty);

    public void BindImage(RenderTarget image, Sampler sampler, uint set, uint binding, Span<uint> dynamicOffsets)
        => _backendContext.BindImage(image.BackendRenderTarget.GetImage(_backendContext.FrameContext.CurrentSwapchainImageIndex), sampler.BackendSampler, set, binding, dynamicOffsets);

    public void BindImage(RenderTarget image, Sampler sampler, uint set, uint binding)
        => _backendContext.BindImage(image.BackendRenderTarget.GetImage(_backendContext.FrameContext.CurrentSwapchainImageIndex), sampler.BackendSampler, set, binding, Span<uint>.Empty);

    public void SetPushConstants<T>(ShaderStageFlags stageFlags, uint offset, T[] data) where T : unmanaged
        => _backendContext.SetPushConstants(stageFlags, offset, data);

    public void SetPushConstants<T>(ShaderStageFlags stageFlags, uint offset, T data) where T : unmanaged
        => _backendContext.SetPushConstants(stageFlags, offset, data);
}