using OpenTK;
using OpenTK.Mathematics;
using Vortice.Vulkan;

namespace Engine.Vulkan;

internal static class VulkanColorExtensions
{
    public static VkClearColorValue ToVkClearColorValue(this Color3<Rgb> color)
        => new(color.X, color.Y, color.Z);
    
    public static VkRect2D ToVkRect2D(this Box2i box)
    {
        return new VkRect2D
        {
            extent = new VkExtent2D
            {
                width = (uint)box.Width,
                height = (uint)box.Height,
            },
            offset = new VkOffset2D
            {
                x = box.Left,
                y = box.Top,
            },
        };
    }

    public static Box2i ToBox2i(this VkRect2D box)
        => new(box.offset.x, box.offset.y, (int)box.extent.width, (int)box.extent.height);
    
    public static Vector2i ToVector2i(this VkExtent2D extent)
        => new((int)extent.width, (int)extent.height);
    
    public static VkExtent2D ToVkExtent2D(this Vector2i extent)
        => new((uint)extent.X, (uint)extent.Y);
}