namespace Engine;

public abstract class BackendImage
{
    public uint Width { get; protected set; }
    public uint Height { get; protected set; }
    public abstract void Dispose();
}