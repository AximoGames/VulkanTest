namespace Engine.Vulkan;

public static class VulkanGraphicsFactory
{
    public static BackendDevice CreateVulkanGraphicsDevice(string applicationName, bool enableValidation, Window window)
    {
        return new VulkanDevice(applicationName, enableValidation, window);
    }
}