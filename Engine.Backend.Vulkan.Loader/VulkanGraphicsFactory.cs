namespace Engine.Vulkan;

public static class VulkanGraphicsFactory
{
    public static BackendDevice CreateVulkanGraphicsDevice(Window window, string applicationName, bool enableValidation, IEnumerable<string>? suppressDebugMessages = null)
        => new VulkanDevice(window, applicationName, enableValidation, suppressDebugMessages);
}