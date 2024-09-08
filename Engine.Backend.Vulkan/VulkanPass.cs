namespace Engine.Vulkan;

public class VulkanPass : BackendPass
{
    public AttachmentDescription ColorAttachment { get; }
    public AttachmentDescription DepthStencilAttachment { get; }
    public BackendRenderTarget RenderTarget { get; }

    public VulkanPass(AttachmentDescription colorAttachment, AttachmentDescription depthStencilAttachment, BackendRenderTarget renderTarget)
    {
        ColorAttachment = colorAttachment;
        DepthStencilAttachment = depthStencilAttachment;
        RenderTarget = renderTarget;
    }
}