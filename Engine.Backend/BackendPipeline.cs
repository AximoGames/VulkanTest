namespace Engine;

public abstract class BackendPipeline : IDisposable
{
    public BackendGraphicsDevice GraphicsDevice { get; private set; }
    public abstract void Dispose();

    public BackendPipeline(BackendGraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;
    }
}