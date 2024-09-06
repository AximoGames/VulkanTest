using System.Runtime.CompilerServices;

namespace Engine;

public abstract class BackendDevice : IDisposable
{
  public abstract void Dispose();
    public abstract BackendPipelineBuilder CreatePipelineBuilder();
    // public abstract BackendBuffer CreateBuffer<T>(BufferType bufferType, int count) where T : unmanaged;
    // public abstract void CopyBuffer<T>(T[] source, int sourceStartIndex, BackendBuffer destinationBuffer, int destinationStartIndex, int count) where T : unmanaged;
    public abstract BackendBufferManager BackendBufferManager { get; }
    public abstract void RenderFrame(Action<BackendRenderContext> draw, BackendPipeline pipeline, [CallerMemberName] string? frameName = null);
}