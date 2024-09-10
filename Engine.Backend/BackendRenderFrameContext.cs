namespace Engine;

public abstract class BackendRenderFrameContext
{
    public abstract void UsePass(BackendPass pass, Action<BackendUsePassContext> action);
    public abstract BackendDevice Device { get; }
    public abstract uint CurrentSwapchainImageIndex { get; }
}