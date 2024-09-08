namespace Engine;

public class PipelineBuilder
{
    private readonly BackendPipelineBuilder _backendPipelineBuilder;

    internal PipelineBuilder(BackendPipelineBuilder backendPipelineBuilder)
    {
        _backendPipelineBuilder = backendPipelineBuilder;
    }

    public void ConfigureShader(string shaderCode, ShaderKind shaderKind)
    {
        _backendPipelineBuilder.ConfigureShader(shaderCode, shaderKind);
    }

    public void ConfigureVertexLayout(VertexLayoutInfo vertexLayoutInfo)
    {
        _backendPipelineBuilder.ConfigureVertexLayout(vertexLayoutInfo);
    }

    public void ConfigurePipelineLayout(PipelineLayoutDescription layoutDescription)
    {
        _backendPipelineBuilder.ConfigurePipelineLayout(layoutDescription);
    }

    internal Pipeline Build()
    {
        return new Pipeline(_backendPipelineBuilder.Build());
    }
}