namespace Engine;

public class ResourceAllocator
{
    private readonly BackendBufferManager _backendBufferManager;

    internal ResourceAllocator(BackendBufferManager backendBufferManager)
    {
        _backendBufferManager = backendBufferManager;
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
}