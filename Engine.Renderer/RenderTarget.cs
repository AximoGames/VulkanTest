using OpenTK.Mathematics;

namespace Engine;

public class RenderTarget
{
    internal BackendRenderTarget BackendRenderTarget { get; }

    internal RenderTarget(BackendRenderTarget backendRenderTarget)
    {
        BackendRenderTarget = backendRenderTarget;
    }

    public Vector2i Extent => BackendRenderTarget.Extent;
}