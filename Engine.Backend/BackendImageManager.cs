namespace Engine;

public abstract class BackendImageManager
{
    public abstract BackendImage CreateRenderTargetImage(uint width, uint height);
}