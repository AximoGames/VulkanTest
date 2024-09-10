using OpenTK;
using OpenTK.Mathematics;

namespace Engine;

public abstract class BackendUsePassContext
{
    public abstract BackendRenderFrameContext FrameContext { get; }

    public abstract void UsePipeline(BackendPipeline pipeline, Action<BackendRenderContext> action);
    public abstract void Clear(Color3<Rgb> clearColor, Box2i rect);
}