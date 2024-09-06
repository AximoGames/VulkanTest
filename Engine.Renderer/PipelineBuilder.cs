namespace Engine;

public class PipelineBuilder
{
    private readonly BackendPipelineBuilder _backendPipelineBuilder;

    internal PipelineBuilder(BackendPipelineBuilder pipelineBuilder)
    {
        _backendPipelineBuilder = pipelineBuilder;
    }

    public void ConfigureShader(string shaderCode, ShaderKind shaderKind)
    {
        _backendPipelineBuilder.ConfigureShader(shaderCode, shaderKind);
    }

    public Buffer CreateVertexBuffer<T>(T[] vertices) where T : unmanaged
    {
        var backendBuffer = _backendPipelineBuilder.CreateVertexBuffer(vertices);
        return new Buffer(backendBuffer);
    }

    public Buffer CreateIndexBuffer(ushort[] indices)
    {
        var backendBuffer = _backendPipelineBuilder.CreateIndexBuffer(indices);
        return new Buffer(backendBuffer);
    }

    public void ConfigureVertexLayout(VertexLayoutInfo vertexLayoutInfo)
    {
        _backendPipelineBuilder.ConfigureVertexLayout(vertexLayoutInfo);
    }

    internal Action<BackendPipelineBuilder> Build => builder => 
    {
        // Transfer any additional configuration from this builder to the backend builder
        // This might include transferring shader configurations, vertex layouts, etc.
    };
}