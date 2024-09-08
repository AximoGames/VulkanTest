namespace Engine;

public abstract class BackendPassBuilder
{
    public abstract void ConfigureColorAttachment(AttachmentDescription attachmentDescription);
    public abstract void ConfigureDepthStencilAttachment(AttachmentDescription attachmentDescription);
    public abstract void SetRenderTarget(BackendRenderTarget renderTarget);
    public abstract BackendPass Build();
}