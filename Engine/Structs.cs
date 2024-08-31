using System.Collections.Generic;

namespace Engine;

public class VertexLayoutInfo
{
    public VertexInputBindingDescription BindingDescription { get; set; }
    public List<VertexInputAttributeDescription> AttributeDescriptions { get; set; }

    public VertexLayoutInfo()
    {
        AttributeDescriptions = new List<VertexInputAttributeDescription>();
    }
}

public struct VertexInputBindingDescription
{
    public uint Binding { get; set; }
    public uint Stride { get; set; }
    public VertexInputRate InputRate { get; set; }
}

public struct VertexInputAttributeDescription
{
    public uint Location { get; set; }
    public uint Binding { get; set; }
    public VertexFormat Format { get; set; }
    public uint Offset { get; set; }
}

public enum VertexInputRate
{
    Vertex,
    Instance
}

public enum VertexFormat
{
    Float32_2,
    Float32_3,
    Float32_4,
    // Add more formats as needed
}