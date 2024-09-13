using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine;

public abstract class BackendImageManager
{
    public abstract BackendRenderTarget CreateImageRenderTarget(Vector2i extent);
    public abstract BackendImage CreateImage<T>(Span<T> source, Vector2i extend) where T : unmanaged;
    public abstract BackendSampler CreateSampler(SamplerDescription description);
    
    public BackendImage CreateImage(Image<Bgra32> source)
    {
        // Convert image to byte array
        byte[] imageData = new byte[source.Width * source.Height * 4];
        source.CopyPixelDataTo(imageData);
        return CreateImage(imageData.AsSpan(), new Vector2i(source.Width, source.Height));
    }

}
