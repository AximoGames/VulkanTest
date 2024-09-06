using System.Runtime.CompilerServices;

namespace Engine;

public class GraphicsDevice : IDisposable
{
    private readonly BackendGraphicsDevice _backendGraphicsDevice;
    // private Pipeline _pipeline;
    private ResourceAllocator _resourceAllocator;

    public GraphicsDevice(BackendGraphicsDevice backendGraphicsDevice)
    {
        _backendGraphicsDevice = backendGraphicsDevice;
        _resourceAllocator = new ResourceAllocator(_backendGraphicsDevice.BackendBufferManager);
    }

    public void Dispose()
        => _backendGraphicsDevice?.Dispose();

    public Pipeline CreatePipeline(Action<PipelineBuilder> callback)
    {
        var pipelineBuilder = new PipelineBuilder(_backendGraphicsDevice.CreatePipelineBuilder());
        callback(pipelineBuilder);
        return pipelineBuilder.Build();
    }

    public void InitializeResources(Action<ResourceAllocator> callback)
    {
        callback(_resourceAllocator);
    }
}