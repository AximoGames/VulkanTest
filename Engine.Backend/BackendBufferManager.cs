namespace Engine;

public abstract class BackendBufferManager : IDisposable
{
    public abstract BackendBuffer CreateBuffer<T>(BufferType bufferType, int elementCount) where T : unmanaged;
    public abstract void CopyBuffer<T>(Span<T> source, int sourceStartIndex, BackendBuffer destinationBuffer, int destinationStartIndex, int count) where T : unmanaged;
    public abstract void Dispose();
    public abstract BackendBuffer CreateUniformBuffer<T>() where T : unmanaged;
    public abstract void UpdateUniformBuffer<T>(BackendBuffer buffer, T data) where T : unmanaged;
}