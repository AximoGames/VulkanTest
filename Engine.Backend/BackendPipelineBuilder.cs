namespace Engine;

public abstract class BackendPipelineBuilder
{
    public abstract void ConfigureShader(string shaderCode, ShaderKind shaderKind);
    public abstract BackendBuffer CreateVertexBuffer<T>(T[] vertices) where T : unmanaged;
    public abstract BackendBuffer CreateIndexBuffer(ushort[] indices);
    public abstract void ConfigureVertexLayout(VertexLayoutInfo vertexLayoutInfo);
    public abstract BackendPipeline Build();
}