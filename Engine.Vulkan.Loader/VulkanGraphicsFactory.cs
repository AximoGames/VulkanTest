using OpenTK.Windowing.Desktop;

namespace Engine.Vulkan;

public static class VulkanGraphicsFactory
{
    public static GraphicsDevice CreateVulkanGraphicsDevice(string applicationName, bool enableValidation, GameWindow window)
    {
        return new VulkanGraphicsDevice(applicationName, enableValidation, window);
    }
}