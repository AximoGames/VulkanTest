namespace Engine;

public class PipelineBuilder
{
    public void ConfigureShader(string shaderCode, ShaderKind shaderKind)
    {
        throw new NotImplementedException();
    }

    public Buffer CreateVertexBuffer<T>(T[] vertices) where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public Buffer CreateIndexBuffer(ushort[] indices)
    {
        throw new NotImplementedException();
    }

    public void ConfigureVertexLayout(VertexLayoutInfo vertexLayoutInfo)
    {
        throw new NotImplementedException();
    }
}