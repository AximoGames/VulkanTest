namespace Engine;

public class DrawFrameContext
{
    private readonly BackendDevice _backendDevice;
    private readonly BackendRenderFrameContext _backendContext;

    public DrawFrameContext(BackendRenderFrameContext backendContext)
    {
        _backendContext = backendContext;
    }

    public void UsePass(Pass pass, Action<UsePassContext> action)
    {
        //TODO: Pass
        _backendContext.UsePass(backendContext => action(new UsePassContext(backendContext)));
    }
}