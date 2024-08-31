using System.Collections.Generic;
using OpenTK.Mathematics;
using Vortice.ShaderCompiler;

namespace Engine.Vulkan;

public abstract class PipelineBuilder
{
    public abstract void ConfigureShader(string shaderCode, ShaderKind shaderKind);
    public abstract Buffer CreateVertexBuffer<T>(T[] vertices) where T : unmanaged;
    public abstract Buffer CreateIndexBuffer(ushort[] indices);
}