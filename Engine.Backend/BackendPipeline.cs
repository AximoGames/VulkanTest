namespace Engine;

public abstract class BackendPipeline : IDisposable
{
    public BackendDevice Device { get; private set; }
    public abstract void Dispose();

    public BackendPipeline(BackendDevice device)
    {
        Device = device;
    }
}