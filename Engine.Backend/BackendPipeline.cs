namespace Engine;

public abstract class BackendPipeline : IDisposable
{
    public BackendDevice Device { get; private set; }
    public int PipelineLayoutHash { get; }
    public abstract void Dispose();

    public BackendPipeline(BackendDevice device, int pipelineLayoutHash)
    {
        Device = device;
        PipelineLayoutHash = pipelineLayoutHash;
    }
}