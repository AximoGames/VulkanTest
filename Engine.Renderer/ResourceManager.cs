using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine;

public class ResourceManager
{
    private readonly BackendBufferManager _backendBufferManager;
    private readonly BackendImageManager _backendImageManager;

    internal ResourceManager(BackendBufferManager backendBufferManager, BackendImageManager backendImageManager)
    {
        _backendBufferManager = backendBufferManager;
        _backendImageManager = backendImageManager;
    }

    public Buffer CreateVertexBuffer<T>(T[] vertices) where T : unmanaged
    {
        var backendBuffer = _backendBufferManager.CreateBuffer<T>(BufferType.Vertex, vertices.Length);
        _backendBufferManager.CopyBuffer(vertices, 0, backendBuffer, 0, vertices.Length);
        return new Buffer(backendBuffer);
    }

    public Buffer CreateIndexBuffer<T>(T[] indices) where T : unmanaged
    {
        var backendBuffer = _backendBufferManager.CreateBuffer<T>(BufferType.Index, indices.Length);
        _backendBufferManager.CopyBuffer(indices, 0, backendBuffer, 0, indices.Length);
        return new Buffer(backendBuffer);
    }

    public Image CreateImage(Vector2i extent, byte[] pixelData)
    {
        // var backendImage = _backendImageManager.CreateImage(extent);
        // return new Image(backendImage);

        throw new NotImplementedException();
    }

    public Image CreateImage(Image<Rgba32> image)
    {
        var backendImage = _backendImageManager.CreateTextureImage(image);
        return new Image(backendImage);
    }

    public Buffer CreateUniformBuffer<T>() where T : unmanaged
    {
        var backendBuffer = _backendBufferManager.CreateUniformBuffer<T>();
        return new Buffer(backendBuffer);
    }

    public void UpdateUniformBuffer<T>(Buffer buffer, T data) where T : unmanaged
        => _backendBufferManager.UpdateUniformBuffer(buffer.BackendBuffer, data);

    public Sampler CreateSampler(SamplerDescription description)
        => new(_backendImageManager.CreateSampler(description));
}