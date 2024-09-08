using System.Runtime.CompilerServices;

namespace Engine;

public class GraphicsDevice : IDisposable
{
    private readonly BackendDevice _backendDevice;

    // private Pipeline _pipeline;
    private ResourceAllocator _resourceAllocator;

    public GraphicsDevice(BackendDevice backendDevice)
    {
        _backendDevice = backendDevice;
        _resourceAllocator = new ResourceAllocator(_backendDevice.BackendBufferManager, _backendDevice.BackendImageManager);
    }

    public void Dispose()
        => _backendDevice?.Dispose();

    public Pipeline CreatePipeline(Action<PipelineBuilder> callback)
    {
        var pipelineBuilder = new PipelineBuilder(_backendDevice.CreatePipelineBuilder());
        callback(pipelineBuilder);
        return pipelineBuilder.Build();
    }

    public void InitializeResources(Action<ResourceAllocator> callback)
    {
        callback(_resourceAllocator);
    }

    public void RenderFrame(Action<DrawFrameContext> action)
    {
        _backendDevice.RenderFrame(backendContext => action(new DrawFrameContext(backendContext)), "RenderFrame");
    }

    public Pass CreatePass(Action<PassBuilder> callback)
    {
        var passBuilder = new PassBuilder(_backendDevice.CreatePassBuilder());
        callback(passBuilder);
        return passBuilder.Build();
    }

    public RenderTarget GetSwapchainRenderTarget()
    {
        return new RenderTarget(_backendDevice.GetSwapchainRenderTarget());
    }
}