using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanImageRenderTarget : VulkanRenderTarget
{
    private VulkanImage[] _images;

    public VulkanImageRenderTarget(VulkanDevice device, uint width, uint height, uint imageCount, VkFormat format)
        : base(device, width, height, imageCount)
    {
        _images = new VulkanImage[imageCount];
        for (uint i = 0; i < imageCount; i++) 
            _images[i] = CreateImage(format);
    }

    private VulkanImage CreateImage(VkFormat format)
    {
        VkImageCreateInfo imageInfo = new()
        {
            sType = VkStructureType.ImageCreateInfo,
            imageType = VkImageType.Image2D,
            extent = new VkExtent3D { width = Width, height = Height, depth = 1 },
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
        vkCreateImage(Device.LogicalDevice, &imageInfo, null, out image).CheckResult();

        VkMemoryRequirements memRequirements;
        vkGetImageMemoryRequirements(Device.LogicalDevice, image, out memRequirements);

        VkMemoryAllocateInfo allocInfo = new()
        {
            sType = VkStructureType.MemoryAllocateInfo,
            allocationSize = memRequirements.size,
            memoryTypeIndex = Device.VulkanBufferManager.FindMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal)
        };

        VkDeviceMemory imageMemory;
        vkAllocateMemory(Device.LogicalDevice, &allocInfo, null, out imageMemory).CheckResult();

        vkBindImageMemory(Device.LogicalDevice, image, imageMemory, 0).CheckResult();

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
        vkCreateImageView(Device.LogicalDevice, &viewInfo, null, out imageView).CheckResult();

        return new VulkanImage(Device, Width, Height, image, imageView, imageMemory, format, true);
    }

    public override BackendImage GetImage(uint index)
        => _images[index];

    public override void Dispose()
    {
        for (int i = 0; i < _images.Length; i++)
        {
            _images[i].Dispose();
        }
    }
}
