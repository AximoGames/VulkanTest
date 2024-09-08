namespace Engine;

public abstract class BackendUsePassContext
{
    public abstract void UsePipeline(Action<BackendRenderContext> action, BackendPipeline pipeline);
}