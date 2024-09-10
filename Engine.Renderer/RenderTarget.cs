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
    
    public Image GetImage(uint index)
        => new(BackendRenderTarget.GetImage(index)); // TODO: Cache images
}