namespace Engine;

public class UsePassContext
{

    private Pass _pass;
    
    public void UsePipeline(Pipeline pipeline, Action<RenderContext> draw)
    {
        pipeline.RenderFrame(draw); // TODO: Use pass
    }
}