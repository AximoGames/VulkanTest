namespace Engine.Vulkan;

public static class VulkanGraphicsFactory
{
    public static BackendDevice CreateVulkanGraphicsDevice(string applicationName, bool enableValidation, Window window, IEnumerable<string>? suppressDebugMessages = null)
        => new VulkanDevice(applicationName, enableValidation, window, suppressDebugMessages);
}