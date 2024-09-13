namespace Engine;

public abstract class BackendBuffer : IDisposable
{
    protected BackendBuffer(Type elementType, uint size)
    {
        ElementType = elementType;
        Size = size;
    }

    public Type ElementType { get; private set; }
    public uint Size { get; private set; }
    public abstract void Dispose();
}