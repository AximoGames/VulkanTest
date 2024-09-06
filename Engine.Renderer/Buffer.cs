namespace Engine;

public class Buffer
{
    internal BackendBuffer BackendBuffer { get; }

    internal Buffer(BackendBuffer backendBuffer)
    {
        BackendBuffer = backendBuffer;
    }

    public Type ElementType => BackendBuffer.ElementType;
}