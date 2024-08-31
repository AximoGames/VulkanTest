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