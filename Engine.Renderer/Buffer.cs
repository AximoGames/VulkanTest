namespace Engine;

public class Buffer : IDisposable
{
    internal BackendBuffer BackendBuffer { get; private set; }

    internal Buffer(BackendBuffer backendBuffer)
    {
        BackendBuffer = backendBuffer;
    }

    public Type ElementType => BackendBuffer.ElementType;
    public int Size => (int)BackendBuffer.Size;

    public void Dispose()
    {
        BackendBuffer?.Dispose();
        BackendBuffer = null!;
    }
}