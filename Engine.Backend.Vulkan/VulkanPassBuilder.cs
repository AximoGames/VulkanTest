namespace Engine.Vulkan;

internal class VulkanPassBuilder : BackendPassBuilder
{
    private AttachmentDescription _colorAttachment;
    private AttachmentDescription _depthStencilAttachment;

    public override void ConfigureColorAttachment(AttachmentDescription attachmentDescription)
    {
        _colorAttachment = attachmentDescription;
    }

    public override void ConfigureDepthStencilAttachment(AttachmentDescription attachmentDescription)
    {
        _depthStencilAttachment = attachmentDescription;
    }

    public override BackendPass Build()
    {
        return new VulkanPass(_colorAttachment, _depthStencilAttachment);
    }
}