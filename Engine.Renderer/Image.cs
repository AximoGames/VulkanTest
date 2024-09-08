namespace Engine;

public class Image
{
    internal BackendImage BackendImage { get; }

    internal Image(BackendImage backendImage)
    {
        BackendImage = backendImage;
    }

    public uint Width => BackendImage.Width;
    public uint Height => BackendImage.Height;
}