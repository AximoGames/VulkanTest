using System.Runtime.CompilerServices;

namespace Engine;

public class GraphicsDevice : IDisposable
{
    private readonly BackendGraphicsDevice _backendGraphicsDevice;

    public GraphicsDevice(BackendGraphicsDevice backendGraphicsDevice)
    {
        _backendGraphicsDevice = backendGraphicsDevice;
    }

    public void Dispose()
        => _backendGraphicsDevice?.Dispose();

    public void InitializePipeline(Action<PipelineBuilder> callback)
    {
        _backendGraphicsDevice.InitializePipeline(backendBuilder =>
        {
            var pipelineBuilder = new PipelineBuilder(backendBuilder);
            callback(pipelineBuilder);
        });
    }

    public void RenderFrame(Action<RenderContext> draw, [CallerMemberName] string? frameName = null)
    {
        _backendGraphicsDevice.RenderFrame(backendContext =>
        {
            var renderContext = new RenderContext(backendContext);
            draw(renderContext);
        }, frameName);
    }
}