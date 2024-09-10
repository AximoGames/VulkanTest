namespace Engine;

public abstract class BackendUsePassContext
{
    public abstract BackendRenderFrameContext FrameContext { get; }

    public abstract void UsePipeline(BackendPipeline pipeline, Action<BackendRenderContext> action);
}