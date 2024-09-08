namespace Engine;

public class PassBuilder
{
    private readonly BackendPassBuilder _backendPassBuilder;

    internal PassBuilder(BackendPassBuilder backendPassBuilder)
    {
        _backendPassBuilder = backendPassBuilder;
    }

    public void ConfigureColorAttachment(AttachmentDescription attachmentDescription)
    {
        _backendPassBuilder.ConfigureColorAttachment(attachmentDescription);
    }

    public void ConfigureDepthStencilAttachment(AttachmentDescription attachmentDescription)
    {
        _backendPassBuilder.ConfigureDepthStencilAttachment(attachmentDescription);
    }

    internal Pass Build()
    {
        return new Pass(_backendPassBuilder.Build());
    }
}