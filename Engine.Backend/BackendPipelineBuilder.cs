namespace Engine;

public abstract class BackendPipelineBuilder
{
    public abstract void ConfigureShader(string shaderCode, ShaderKind shaderKind);
    public abstract BackendBuffer CreateBuffer<T>(BufferType bufferType, T[] vertices) where T : unmanaged;
    public abstract void ConfigureVertexLayout(VertexLayoutInfo vertexLayoutInfo);
    public abstract BackendPipeline Build();
}