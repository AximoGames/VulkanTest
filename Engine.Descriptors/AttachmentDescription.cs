namespace Engine;

public struct AttachmentDescription
{
    public AttachmentLoadOp LoadOp { get; set; }
    public AttachmentStoreOp StoreOp { get; set; }
    public AttachmentLoadOp StencilLoadOp { get; set; }
    public AttachmentStoreOp StencilStoreOp { get; set; }
    public ImageLayout ImageLayout { get; set; }
}