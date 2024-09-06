using System.Runtime.CompilerServices;

namespace Engine;

public class GraphicsDevice : IDisposable
{
    public GraphicsDevice(BackendGraphicsDevice backendGraphicsDevice)
    {
        BackendGraphicsDevice = backendGraphicsDevice;
    }

    private BackendGraphicsDevice BackendGraphicsDevice { get; set; }

    public void Dispose()
        => BackendGraphicsDevice?.Dispose();

    public void InitializePipeline(Action<PipelineBuilder> callback)
        => throw new NotImplementedException();

    public void RenderFrame(Action<RenderContext> draw, [CallerMemberName] string? frameName = null)
        => throw new NotImplementedException();
}