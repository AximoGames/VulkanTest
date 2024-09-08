namespace Engine;

public abstract class BackendBufferManager : IDisposable
{
    public abstract BackendBuffer CreateBuffer<T>(BufferType bufferType, int elementCount) where T : unmanaged;
    public abstract void CopyBuffer<T>(T[] source, int sourceStartIndex, BackendBuffer destinationBuffer, int destinationStartIndex, int count) where T : unmanaged;
    public abstract void Dispose();
}