using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanImage : BackendImage
{
    private readonly VulkanDevice _device;
    internal VkImage Image { get; }
    internal VkImageView ImageView { get; }
    internal VkDeviceMemory Memory { get; }
    internal VkFormat Format { get; }
    internal bool IsRenderTarget { get; }

    public VulkanImage(VulkanDevice device, uint width, uint height, VkImage image, VkImageView imageView, VkDeviceMemory memory, VkFormat format, bool isRenderTarget)
    {
        _device = device;
        Width = width;
        Height = height;
        Image = image;
        ImageView = imageView;
        Memory = memory;
        Format = format;
        IsRenderTarget = isRenderTarget;
    }

    public override void Dispose()
    {
        vkDestroyImageView(_device.LogicalDevice, ImageView, null);
        vkDestroyImage(_device.LogicalDevice, Image, null);
        vkFreeMemory(_device.LogicalDevice, Memory, null);
    }
}