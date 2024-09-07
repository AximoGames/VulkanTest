namespace Engine;

public class UsePassContext
{
    private Pass _pass;
    private BackendUsePassContext _backendContext;

    public UsePassContext(BackendUsePassContext backendContext)
    {
        _backendContext = backendContext;
    }

    public void UsePipeline(Pipeline pipeline, Action<RenderPipelineContext> draw)
    {
        _backendContext.UsePipeline(backendContext =>
        {
            var drawContext = new RenderPipelineContext(backendContext);
            draw(drawContext);
        }, pipeline.BackendPipeline);
    }
}