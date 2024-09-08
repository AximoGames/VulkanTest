using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using OpenTK.Mathematics;

namespace Engine.Vulkan;

internal unsafe class VulkanImageManager : BackendImageManager
{
    private readonly VulkanDevice _device;
    private readonly VulkanBufferManager _bufferManager;

    public VulkanImageManager(VulkanDevice device, VulkanBufferManager bufferManager)
    {
        _device = device;
        _bufferManager = bufferManager;
    }

    public override BackendImage CreateRenderTargetImage(Vector2i extent)
    {
        VkImageCreateInfo imageInfo = new VkImageCreateInfo
        {
            imageType = VkImageType.Image2D,
            extent = new VkExtent3D { width = (uint)extent.X, height = (uint)extent.Y, depth = 1 },
            mipLevels = 1,
            arrayLayers = 1,
            format = VkFormat.R8G8B8A8Unorm,
            tiling = VkImageTiling.Optimal,
            initialLayout = VkImageLayout.Undefined,
            usage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferSrc,
            sharingMode = VkSharingMode.Exclusive,
            samples = VkSampleCountFlags.Count1
        };

        VkImage image;
        vkCreateImage(_device.LogicalDevice, &imageInfo, null, out image).CheckResult();

        VkMemoryRequirements memRequirements;
        vkGetImageMemoryRequirements(_device.LogicalDevice, image, out memRequirements);

        VkMemoryAllocateInfo allocInfo = new VkMemoryAllocateInfo
        {
            allocationSize = memRequirements.size,
            memoryTypeIndex = _bufferManager.FindMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal)
        };

        VkDeviceMemory imageMemory;
        vkAllocateMemory(_device.LogicalDevice, &allocInfo, null, out imageMemory).CheckResult();

        vkBindImageMemory(_device.LogicalDevice, image, imageMemory, 0).CheckResult();

        VkImageViewCreateInfo viewInfo = new VkImageViewCreateInfo
        {
            image = image,
            viewType = VkImageViewType.Image2D,
            format = VkFormat.R8G8B8A8Unorm,
            subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = 0,
                levelCount = 1,
                baseArrayLayer = 0,
                layerCount = 1
            }
        };

        VkImageView imageView;
        vkCreateImageView(_device.LogicalDevice, &viewInfo, null, out imageView).CheckResult();

        return new VulkanImage(_device, extent, image, imageView, imageMemory, VkFormat.R8G8B8A8Unorm, true);
    }
}
