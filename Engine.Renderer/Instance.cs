namespace Engine;

public class Instance : IDisposable
{
    private readonly BackendInstance _backendInstance;

    internal Instance(BackendInstance backendInstance)
    {
        _backendInstance = backendInstance;
    }

    public void Dispose()
        => _backendInstance?.Dispose();

    public Device CreateDevice(Window window)
    {
        return new Device(_backendInstance.CreateDevice(window));
    }
}