using System.Runtime.CompilerServices;

namespace Engine.Vulkan;

public abstract class GraphicsDevice : IDisposable
{
    public abstract void Dispose();
    public abstract void InitializePipeline(Action<PipelineBuilder> callback);
    public abstract void RenderFrame(Action<RenderContext> draw, [CallerMemberName] string? frameName = null);
}