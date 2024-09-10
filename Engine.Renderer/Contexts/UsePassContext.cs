using OpenTK;
using OpenTK.Mathematics;

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
        _backendContext.UsePipeline(pipeline.BackendPipeline, backendContext =>
        {
            var drawContext = new RenderPipelineContext(backendContext);
            draw(drawContext);
        });
    }
    
    public void Clear(Color3<Rgb> clearColor)
        => _backendContext.Clear(clearColor, new Box2i(Vector2i.Zero, _backendContext.FrameContext.Device.GetSwapchainRenderTarget().Extent));

    public void Clear(Color3<Rgb> clearColor, Box2i rect)
        => _backendContext.Clear(clearColor, rect);

}