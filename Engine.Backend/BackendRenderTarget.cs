using OpenTK.Mathematics;

namespace Engine;

public abstract class BackendRenderTarget : IDisposable
{
    public abstract Vector2i Extent { get; }
    public abstract void Dispose();
    public abstract BackendImage GetImage(uint index);
    public abstract uint ImageCount { get; }
}