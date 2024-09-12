namespace Engine.Vulkan;

public class VulkanFactory : GraphicsFactory
{
    public override Instance CreateInstance(WindowManager windowManager, string applicationName, bool enableValidation = true, IEnumerable<string>? suppressDebugMessages = null)
        => new(new VulkanInstance(applicationName, enableValidation, windowManager, suppressDebugMessages));
}