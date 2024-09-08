namespace Engine;

public abstract class BackendTexture
{
    public uint Width { get; protected set; }
    public uint Height { get; protected set; }
    public abstract void Dispose();
}