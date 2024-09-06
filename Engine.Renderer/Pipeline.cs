using System.Runtime.CompilerServices;

namespace Engine;

public class Pipeline
{
    internal BackendPipeline BackendPipeline { get; }

    internal Pipeline(BackendPipeline backendPipeline)
    {
        BackendPipeline = backendPipeline;
    }

    public void RenderFrame(Action<RenderContext> draw, [CallerMemberName] string? frameName = null)
    {
        BackendPipeline.GraphicsDevice.RenderFrame(backendContext =>
        {
            var drawContext = new RenderContext(backendContext);
            draw(drawContext);
        }, BackendPipeline, frameName);
    }
}