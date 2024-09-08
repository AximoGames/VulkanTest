using OpenTK.Mathematics;

namespace Engine;

public abstract class BackendImage
{
    public abstract Vector2i Extent { get; }
    public abstract void Dispose();
}