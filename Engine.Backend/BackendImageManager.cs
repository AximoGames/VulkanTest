using OpenTK.Mathematics;

namespace Engine;

public abstract class BackendImageManager
{
    public abstract BackendImage CreateRenderTargetImage(Vector2i extent);
}