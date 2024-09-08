namespace Engine;

public abstract class BackendUsePassContext
{
    public abstract void UsePipeline(BackendPipeline pipeline, Action<BackendRenderContext> action);
}