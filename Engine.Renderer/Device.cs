using System.Runtime.CompilerServices;

namespace Engine;

public class Device : IDisposable
{
    private readonly BackendDevice _backendDevice;

    // private Pipeline _pipeline;
    private ResourceManager _resourceManager;

    public Device(BackendDevice backendDevice)
    {
        _backendDevice = backendDevice;
        _resourceManager = new ResourceManager(_backendDevice.BackendBufferManager, _backendDevice.BackendImageManager);
    }

    public void Dispose()
        => _backendDevice?.Dispose();

    public Pipeline CreatePipeline(Action<PipelineBuilder> callback)
    {
        PipelineBuilder pipelineBuilder = new PipelineBuilder(_backendDevice.CreatePipelineBuilder());
        callback(pipelineBuilder);
        return pipelineBuilder.Build();
    }

    public void InitializeResources(Action<ResourceManager> callback)
    {
        callback(_resourceManager);
    }

    public void RenderFrame(Action<DrawFrameContext> action)
    {
        _backendDevice.RenderFrame(backendContext => action(new DrawFrameContext(backendContext)), "RenderFrame");
    }
    
    public Pass CreatePass(Action<PassBuilder> callback)
    {
        PassBuilder passBuilder = new PassBuilder(_backendDevice.CreatePassBuilder());
        callback(passBuilder);
        return passBuilder.Build();
    }

    public RenderTarget GetSwapchainRenderTarget()
    {
        return new RenderTarget(_backendDevice.GetSwapchainRenderTarget());
    }
}