using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Engine;

public abstract class PipelineBuilder
{
    public abstract void ConfigureShader(string shaderCode, ShaderKind shaderKind);
    public abstract Buffer CreateVertexBuffer<T>(T[] vertices) where T : unmanaged;
    public abstract Buffer CreateIndexBuffer(ushort[] indices);
    public abstract void ConfigureVertexLayout(VertexLayoutInfo vertexLayoutInfo);
}