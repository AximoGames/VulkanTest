using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine;

public abstract class BackendImageManager
{
    public abstract BackendImage CreateRenderTargetImage(Vector2i extent);
    public abstract BackendImage CreateImage(Image<Rgba32> source);
    public abstract BackendSampler CreateSampler(SamplerDescription description);
}