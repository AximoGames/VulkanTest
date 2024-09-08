namespace Engine.Vulkan;

public class VulkanPass : BackendPass
{
    public AttachmentDescription ColorAttachment { get; }
    public AttachmentDescription DepthStencilAttachment { get; }

    public VulkanPass(AttachmentDescription colorAttachment, AttachmentDescription depthStencilAttachment)
    {
        ColorAttachment = colorAttachment;
        DepthStencilAttachment = depthStencilAttachment;
    }
}