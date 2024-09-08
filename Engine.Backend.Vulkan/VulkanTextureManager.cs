using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanTextureManager : BackendTextureManager
{
    private readonly VulkanDevice _device;
    private readonly VulkanBufferManager _bufferManager;

    public VulkanTextureManager(VulkanDevice device, VulkanBufferManager bufferManager)
    {
        _device = device;
        _bufferManager = bufferManager;
    }

    public override BackendTexture CreateRenderTargetTexture(uint width, uint height)
    {
        VkFormat format = VkFormat.R8G8B8A8Unorm; // You can change this format as needed

        VkImageCreateInfo imageInfo = new()
        {
            sType = VkStructureType.ImageCreateInfo,
            imageType = VkImageType.Image2D,
            extent = new VkExtent3D { width = width, height = height, depth = 1 },
            mipLevels = 1,
            arrayLayers = 1,
            format = format,
            tiling = VkImageTiling.Optimal,
            initialLayout = VkImageLayout.Undefined,
            usage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Sampled,
            sharingMode = VkSharingMode.Exclusive,
            samples = VkSampleCountFlags.Count1
        };

        VkImage image;
        vkCreateImage(_device.LogicalDevice, &imageInfo, null, out image).CheckResult();

        VkMemoryRequirements memRequirements;
        vkGetImageMemoryRequirements(_device.LogicalDevice, image, out memRequirements);

        VkMemoryAllocateInfo allocInfo = new()
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = memRequirements.size,
            memoryTypeIndex = _bufferManager.FindMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal)
        };

        VkDeviceMemory imageMemory;
        vkAllocateMemory(_device.LogicalDevice, &allocInfo, null, out imageMemory).CheckResult();

        vkBindImageMemory(_device.LogicalDevice, image, imageMemory, 0).CheckResult();

        VkImageViewCreateInfo viewInfo = new()
        {
            sType = VkStructureType.ImageViewCreateInfo,
            image = image,
            viewType = VkImageViewType.Image2D,
            format = format,
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

        return new VulkanTexture(_device, width, height, image, imageView, imageMemory, format, true);
    }
}