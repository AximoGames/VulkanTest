using OpenTK.Mathematics;

namespace Engine;

public class Image
{
    internal BackendImage BackendImage { get; }

    internal Image(BackendImage backendImage)
    {
        BackendImage = backendImage;
    }

    public Vector2i Extent => BackendImage.Extent;
}