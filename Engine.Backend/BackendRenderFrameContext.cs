namespace Engine;

public abstract class BackendRenderFrameContext
{
    public abstract void UsePass(BackendPass pass, Action<BackendUsePassContext> action);
}