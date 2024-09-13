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
        => CreateVertexBuffer(vertices.AsSpan());

    public Buffer CreateVertexBuffer<T>(Span<T> vertices) where T : unmanaged
    {
        var backendBuffer = _backendBufferManager.CreateBuffer<T>(BufferType.Vertex, vertices.Length);
        _backendBufferManager.CopyBuffer(vertices, 0, backendBuffer, 0, vertices.Length);
        return new Buffer(backendBuffer);
    }

    public Buffer CreateVertexBuffer<T>(int length) where T : unmanaged
        => new(_backendBufferManager.CreateBuffer<T>(BufferType.Vertex, length));

    public Buffer CreateIndexBuffer<T>(T[] indices) where T : unmanaged
        => CreateIndexBuffer(indices.AsSpan());

    public Buffer CreateIndexBuffer<T>(Span<T> indices) where T : unmanaged
    {
        var backendBuffer = _backendBufferManager.CreateBuffer<T>(BufferType.Index, indices.Length);
        _backendBufferManager.CopyBuffer(indices, 0, backendBuffer, 0, indices.Length);
        return new Buffer(backendBuffer);
    }

    public Buffer CreateIndexBuffer<T>(int length) where T : unmanaged
        => new(_backendBufferManager.CreateBuffer<T>(BufferType.Index, length));

    public Image CreateImage<T>(Span<T> pixelData, Vector2i extent) where T : unmanaged
    {
        var backendImage = _backendImageManager.CreateImage(pixelData, extent);
        return new(backendImage);
    }

    public Image CreateImage<T>(T[] pixelData, Vector2i extent) where T : unmanaged
        => CreateImage(pixelData.AsSpan(), extent);

    public Image CreateImage(Image<Bgra32> image)
    {
        var backendImage = _backendImageManager.CreateImage(image);
        return new(backendImage);
    }

    public RenderTarget CreateImageRenderTarget(Vector2i extent)
        => new(_backendImageManager.CreateImageRenderTarget(extent));

    public Buffer CreateUniformBuffer<T>() where T : unmanaged
    {
        var backendBuffer = _backendBufferManager.CreateUniformBuffer<T>();
        return new Buffer(backendBuffer);
    }

    public void UpdateUniformBuffer<T>(Buffer buffer, T data) where T : unmanaged
        => _backendBufferManager.UpdateUniformBuffer(buffer.BackendBuffer, data);

    public void CopyBuffer<T>(Span<T> source, int sourceStartIndex, Buffer destinationBuffer, int destinationStartIndex, int count) where T : unmanaged
        => _backendBufferManager.CopyBuffer(source, sourceStartIndex, destinationBuffer.BackendBuffer, destinationStartIndex, count);

    public Sampler CreateSampler(SamplerDescription description)
        => new(_backendImageManager.CreateSampler(description));
}