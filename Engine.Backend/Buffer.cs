namespace Engine;

public class Buffer
{
    protected Buffer(Type elementType)
    {
        ElementType = elementType;
    }

    public Type ElementType { get; private set; }
}