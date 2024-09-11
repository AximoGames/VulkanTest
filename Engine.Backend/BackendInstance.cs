namespace Engine;

public abstract class BackendInstance : IDisposable
{
    public abstract BackendDevice CreateDevice(Window window);
    public abstract void Dispose();
}