using System.Collections.Generic;
using OpenTK.Mathematics;
using Vortice.ShaderCompiler;
using Vortice.Vulkan;

namespace Engine.Vulkan;

public abstract class PipelineBuilder
{
    protected VkVertexInputBindingDescription _bindingDescription;
    protected VkVertexInputAttributeDescription[] _attributeDescriptions;

    public abstract void ConfigureShader(string shaderCode, ShaderKind shaderKind);
    public abstract Buffer CreateVertexBuffer<T>(T[] vertices) where T : unmanaged;
    public abstract Buffer CreateIndexBuffer(ushort[] indices);

    public void ConfigureBindingDescription(VkVertexInputBindingDescription bindingDescription)
        => _bindingDescription = bindingDescription;

    public void ConfigureAttributeDescriptions(VkVertexInputAttributeDescription[] attributeDescriptions)
        => _attributeDescriptions = attributeDescriptions;

    public abstract void ConfigureVertexLayout(VertexLayoutInfo vertexLayoutInfo);
}

public enum ShaderKind
{
    VertexShader,
    FragmentShader,
    ComputeShader,
    GeometryShader,
}