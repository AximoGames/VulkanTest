using Vortice.Vulkan;

namespace Engine.Vulkan;

public static class VulkanHelper
{
    public static VkIndexType ToVkIndexType(this Type elementType)
    {
        if (elementType == typeof(UInt16))
            return VkIndexType.Uint16;

        if (elementType == typeof(UInt32))
            return VkIndexType.Uint32;
        
        throw new NotSupportedException($"Unsupported index type: {elementType}");
    }
}