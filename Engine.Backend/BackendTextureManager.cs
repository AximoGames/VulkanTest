namespace Engine;

public abstract class BackendTextureManager
{
    public abstract BackendTexture CreateRenderTargetTexture(uint width, uint height);
}