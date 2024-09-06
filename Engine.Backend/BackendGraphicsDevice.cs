using System.Runtime.CompilerServices;

namespace Engine;

public abstract class BackendGraphicsDevice : IDisposable
{
    public abstract void Dispose();
    public abstract BackendPipelineBuilder CreatePipelineBuilder();
    public abstract void InitializePipeline(Action<BackendPipelineBuilder> callback);
    public abstract void RenderFrame(Action<BackendRenderContext> draw, [CallerMemberName] string? frameName = null);
}