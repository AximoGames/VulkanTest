namespace Engine;

public abstract class GraphicsFactory
{
    public abstract Instance CreateInstance(WindowManager windowManager, string applicationName, bool enableValidation, IEnumerable<string>? suppressDebugMessages = null);
}