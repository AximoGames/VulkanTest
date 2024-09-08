namespace Engine;

public class Texture
{
    internal BackendTexture BackendTexture { get; }

    internal Texture(BackendTexture backendTexture)
    {
        BackendTexture = backendTexture;
    }

    public uint Width => BackendTexture.Width;
    public uint Height => BackendTexture.Height;
}