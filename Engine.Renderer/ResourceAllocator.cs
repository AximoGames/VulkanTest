namespace Engine;

public class ResourceAllocator
{
    private readonly BackendBufferManager _backendBufferManager;
    private readonly BackendTextureManager _backendTextureManager;

    internal ResourceAllocator(BackendBufferManager backendBufferManager, BackendTextureManager backendTextureManager)
    {
        _backendBufferManager = backendBufferManager;
        _backendTextureManager = backendTextureManager;
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

    public Texture CreateTexture(uint width, uint height, byte[] pixelData)
    {
        var backendTexture = _backendTextureManager.CreateRenderTargetTexture(width, height);
        return new Texture(backendTexture);
    }
}