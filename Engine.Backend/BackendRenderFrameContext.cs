namespace Engine;

public abstract class BackendRenderFrameContext
{
    public abstract void UsePass(Action<BackendUsePassContext> action);
}