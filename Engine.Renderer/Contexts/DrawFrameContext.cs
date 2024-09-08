namespace Engine;

public class DrawFrameContext
{
    private readonly BackendDevice _backendDevice;
    private readonly BackendRenderFrameContext _backendContext;
    public ResourceManager ResourceManager { get; }

    internal DrawFrameContext(BackendRenderFrameContext backendContext)
    {
        _backendContext = backendContext;
        _backendDevice = backendContext.Device;
        ResourceManager = new ResourceManager(_backendDevice.BackendBufferManager, _backendDevice.BackendImageManager);
    }

    public void UsePass(Pass pass, Action<UsePassContext> action)
    {
        //TODO: Pass
        _backendContext.UsePass(pass?.BackendPass, backendContext => action(new UsePassContext(backendContext)));
    }
}