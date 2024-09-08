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

public struct AttachmentDescription
{
    public AttachmentLoadOp LoadOp { get; set; }
    public AttachmentStoreOp StoreOp { get; set; }
    public AttachmentLoadOp StencilLoadOp { get; set; }
    public AttachmentStoreOp StencilStoreOp { get; set; }
    public ImageLayout InitialLayout { get; set; }
    public ImageLayout FinalLayout { get; set; }
}

public enum AttachmentLoadOp
{
    Load,
    Clear,
    DontCare
}

public enum AttachmentStoreOp
{
    Store,
    DontCare
}

public enum ImageLayout
{
    Undefined,
    General,
    ColorAttachmentOptimal,
    DepthStencilAttachmentOptimal,
    DepthStencilReadOnlyOptimal,
    ShaderReadOnlyOptimal,
    TransferSrcOptimal,
    TransferDstOptimal,
    Preinitialized,
    PresentSrcKHR
}