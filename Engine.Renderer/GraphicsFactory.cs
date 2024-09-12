namespace Engine;

public abstract class GraphicsFactory
{
    public abstract Instance CreateInstance(WindowManager windowManager, string applicationName, bool enableValidation = true, IEnumerable<string>? suppressDebugMessages = null);
}