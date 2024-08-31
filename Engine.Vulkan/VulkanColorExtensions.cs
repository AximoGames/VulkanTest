using OpenTK;
using Vortice.Vulkan;

namespace Engine.Vulkan;

public static class VulkanColorExtensions
{
    public static VkClearColorValue ToVkClearColorValue(this Color3<Rgb> color)
        => new(color.X, color.Y, color.Z);
}