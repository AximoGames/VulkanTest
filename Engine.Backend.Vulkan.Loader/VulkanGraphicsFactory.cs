namespace Engine.Vulkan;

public static class VulkanGraphicsFactory
{
    public static BackendGraphicsDevice CreateVulkanGraphicsDevice(string applicationName, bool enableValidation, Window window)
    {
        return new VulkanBackendGraphicsDevice(applicationName, enableValidation, window);
    }
}