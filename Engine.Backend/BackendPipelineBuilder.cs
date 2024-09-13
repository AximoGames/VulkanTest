namespace Engine;

public abstract class BackendPipelineBuilder
{
    public abstract void ConfigureShader(string shaderCode, ShaderKind shaderKind);
    public abstract BackendBuffer CreateBuffer<T>(BufferType bufferType, int count) where T : unmanaged;
    public abstract void CopyBuffer<T>(Span<T> source, int sourceStartIndex, BackendBuffer destinationBuffer, int destinationStartIndex, int count) where T : unmanaged;
    public abstract void ConfigureVertexLayout(VertexLayoutInfo vertexLayoutInfo);
    public abstract void ConfigurePipelineLayout(PipelineLayoutDescription layoutDescription);
    public abstract void ConfigurePushConstants(uint size, ShaderStageFlags stageFlags);
    public abstract BackendPipeline Build();
}