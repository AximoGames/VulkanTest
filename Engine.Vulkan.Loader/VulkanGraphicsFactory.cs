namespace Engine.Vulkan;

public static class VulkanGraphicsFactory
{
    public static GraphicsDevice CreateVulkanGraphicsDevice(string applicationName, bool enableValidation, Window window)
    {
        return new VulkanGraphicsDevice(applicationName, enableValidation, window);
    }
}