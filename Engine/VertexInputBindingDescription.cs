namespace Engine;

public struct VertexInputBindingDescription
{
    public uint Binding { get; set; }
    public uint Stride { get; set; }
    public VertexInputRate InputRate { get; set; }
}