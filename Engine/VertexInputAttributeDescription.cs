namespace Engine;

public struct VertexInputAttributeDescription
{
    public uint Location { get; set; }
    public uint Binding { get; set; }
    public VertexFormat Format { get; set; }
    public uint Offset { get; set; }
}