namespace Engine;

public class BackendBuffer
{
    protected BackendBuffer(Type elementType)
    {
        ElementType = elementType;
    }

    public Type ElementType { get; private set; }
}