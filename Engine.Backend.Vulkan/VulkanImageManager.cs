using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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

    public override BackendImage CreateImage(Image<Rgba32> source)
    {
        // Convert image to byte array
        byte[] imageData = new byte[source.Width * source.Height * 4];
        source.CopyPixelDataTo(imageData);

        // Create staging buffer
        uint imageSize = (uint)(imageData.Length);
        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        _bufferManager.CreateBuffer(imageSize, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out stagingBuffer, out stagingBufferMemory);

        // Copy image data to staging buffer
        unsafe
        {
            void* data;
            vkMapMemory(_device.LogicalDevice, stagingBufferMemory, 0, imageSize, 0, &data);
            fixed (byte* imageDataPtr = imageData)
            {
                Buffer.MemoryCopy(imageDataPtr, data, imageSize, imageSize);
            }
            vkUnmapMemory(_device.LogicalDevice, stagingBufferMemory);
        }

        // Create image
        VkImage image;
        VkDeviceMemory imageMemory;
        VkExtent3D imageExtent = new VkExtent3D { width = (uint)source.Width, height = (uint)source.Height, depth = 1 };

        CreateImage(
            imageExtent,
            VkFormat.R8G8B8A8Unorm,
            VkImageTiling.Optimal,
            VkImageUsageFlags.TransferDst | VkImageUsageFlags.Sampled,
            VkMemoryPropertyFlags.DeviceLocal,
            out image,
            out imageMemory
        );

        // Transition image layout for copy
        TransitionImageLayout(image, VkFormat.R8G8B8A8Unorm, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);

        // Copy buffer to image
        CopyBufferToImage(stagingBuffer, image, (uint)source.Width, (uint)source.Height);

        // Transition image layout for shader access
        TransitionImageLayout(image, VkFormat.R8G8B8A8Unorm, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);

        // Clean up staging buffer
        vkDestroyBuffer(_device.LogicalDevice, stagingBuffer, null);
        vkFreeMemory(_device.LogicalDevice, stagingBufferMemory, null);

        // Create image view
        VkImageView imageView = CreateImageView(image, VkFormat.R8G8B8A8Unorm, VkImageAspectFlags.Color);

        return new VulkanImage(_device, new Vector2i(source.Width, source.Height), image, imageView, imageMemory, VkFormat.R8G8B8A8Unorm, false);
    }

    private void CreateImage(VkExtent3D extent, VkFormat format, VkImageTiling tiling, VkImageUsageFlags usage, VkMemoryPropertyFlags properties, out VkImage image, out VkDeviceMemory imageMemory)
    {
        VkImageCreateInfo imageInfo = new VkImageCreateInfo
        {
            imageType = VkImageType.Image2D,
            extent = extent,
            mipLevels = 1,
            arrayLayers = 1,
            format = format,
            tiling = tiling,
            initialLayout = VkImageLayout.Undefined,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive,
            samples = VkSampleCountFlags.Count1
        };

        vkCreateImage(_device.LogicalDevice, &imageInfo, null, out image).CheckResult();

        VkMemoryRequirements memRequirements;
        vkGetImageMemoryRequirements(_device.LogicalDevice, image, out memRequirements);

        VkMemoryAllocateInfo allocInfo = new VkMemoryAllocateInfo
        {
            allocationSize = memRequirements.size,
            memoryTypeIndex = _bufferManager.FindMemoryType(memRequirements.memoryTypeBits, properties)
        };

        vkAllocateMemory(_device.LogicalDevice, &allocInfo, null, out imageMemory).CheckResult();

        vkBindImageMemory(_device.LogicalDevice, image, imageMemory, 0).CheckResult();
    }

    private void TransitionImageLayout(VkImage image, VkFormat format, VkImageLayout oldLayout, VkImageLayout newLayout)
    {
        VkCommandBuffer commandBuffer = _device.CommandBufferManager.BeginSingleTimeCommands();

        VkImageMemoryBarrier barrier = new VkImageMemoryBarrier
        {
            oldLayout = oldLayout,
            newLayout = newLayout,
            srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            image = image,
            subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = VkImageAspectFlags.Color,
                baseMipLevel = 0,
                levelCount = 1,
                baseArrayLayer = 0,
                layerCount = 1
            }
        };

        VkPipelineStageFlags sourceStage;
        VkPipelineStageFlags destinationStage;

        if (oldLayout == VkImageLayout.Undefined && newLayout == VkImageLayout.TransferDstOptimal)
        {
            barrier.srcAccessMask = 0;
            barrier.dstAccessMask = VkAccessFlags.TransferWrite;

            sourceStage = VkPipelineStageFlags.TopOfPipe;
            destinationStage = VkPipelineStageFlags.Transfer;
        }
        else if (oldLayout == VkImageLayout.TransferDstOptimal && newLayout == VkImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.srcAccessMask = VkAccessFlags.TransferWrite;
            barrier.dstAccessMask = VkAccessFlags.ShaderRead;

            sourceStage = VkPipelineStageFlags.Transfer;
            destinationStage = VkPipelineStageFlags.FragmentShader;
        }
        else
        {
            throw new InvalidOperationException("Unsupported layout transition!");
        }

        vkCmdPipelineBarrier(
            commandBuffer,
            sourceStage, destinationStage,
            0,
            0, null,
            0, null,
            1, &barrier
        );

        _device.CommandBufferManager.EndSingleTimeCommands(commandBuffer);
    }

    private void CopyBufferToImage(VkBuffer buffer, VkImage image, uint width, uint height)
    {
        VkCommandBuffer commandBuffer = _device.CommandBufferManager.BeginSingleTimeCommands();

        VkBufferImageCopy region = new VkBufferImageCopy
        {
            bufferOffset = 0,
            bufferRowLength = 0,
            bufferImageHeight = 0,
            imageSubresource = new VkImageSubresourceLayers
            {
                aspectMask = VkImageAspectFlags.Color,
                mipLevel = 0,
                baseArrayLayer = 0,
                layerCount = 1
            },
            imageOffset = new VkOffset3D { x = 0, y = 0, z = 0 },
            imageExtent = new VkExtent3D { width = width, height = height, depth = 1 }
        };

        vkCmdCopyBufferToImage(commandBuffer, buffer, image, VkImageLayout.TransferDstOptimal, 1, &region);

        _device.CommandBufferManager.EndSingleTimeCommands(commandBuffer);
    }

    private VkImageView CreateImageView(VkImage image, VkFormat format, VkImageAspectFlags aspectFlags)
    {
        VkImageViewCreateInfo viewInfo = new VkImageViewCreateInfo
        {
            image = image,
            viewType = VkImageViewType.Image2D,
            format = format,
            subresourceRange = new VkImageSubresourceRange
            {
                aspectMask = aspectFlags,
                baseMipLevel = 0,
                levelCount = 1,
                baseArrayLayer = 0,
                layerCount = 1
            }
        };

        VkImageView imageView;
        vkCreateImageView(_device.LogicalDevice, &viewInfo, null, out imageView).CheckResult();

        return imageView;
    }

    public override BackendSampler CreateSampler(SamplerDescription description)
    {
        var samplerCreateInfo = new VkSamplerCreateInfo
        {
            sType = VkStructureType.SamplerCreateInfo,
            magFilter = ConvertFilter(description.MagFilter),
            minFilter = ConvertFilter(description.MinFilter),
            addressModeU = ConvertSamplerAddressMode(description.AddressModeU),
            addressModeV = ConvertSamplerAddressMode(description.AddressModeV),
            addressModeW = ConvertSamplerAddressMode(description.AddressModeW),
            mipLodBias = description.MipLodBias,
            anisotropyEnable = description.AnisotropyEnable,
            maxAnisotropy = description.MaxAnisotropy,
            compareEnable = description.CompareEnable,
            compareOp = ConvertCompareOp(description.CompareOp),
            minLod = description.MinLod,
            maxLod = description.MaxLod,
            borderColor = ConvertBorderColor(description.BorderColor),
            unnormalizedCoordinates = description.UnnormalizedCoordinates
        };

        vkCreateSampler(_device.LogicalDevice, &samplerCreateInfo, null, out VkSampler sampler).CheckResult();
        return new VulkanSampler(_device, sampler);
    }

    private VkFilter ConvertFilter(Filter filter)
    {
        return filter switch
        {
            Filter.Nearest => VkFilter.Nearest,
            Filter.Linear => VkFilter.Linear,
            _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
        };
    }

    private VkSamplerAddressMode ConvertSamplerAddressMode(SamplerAddressMode addressMode)
    {
        return addressMode switch
        {
            SamplerAddressMode.Repeat => VkSamplerAddressMode.Repeat,
            SamplerAddressMode.MirroredRepeat => VkSamplerAddressMode.MirroredRepeat,
            SamplerAddressMode.ClampToEdge => VkSamplerAddressMode.ClampToEdge,
            SamplerAddressMode.ClampToBorder => VkSamplerAddressMode.ClampToBorder,
            _ => throw new ArgumentOutOfRangeException(nameof(addressMode), addressMode, null)
        };
    }

    private VkCompareOp ConvertCompareOp(CompareOp compareOp)
    {
        return compareOp switch
        {
            CompareOp.Never => VkCompareOp.Never,
            CompareOp.Less => VkCompareOp.Less,
            CompareOp.Equal => VkCompareOp.Equal,
            CompareOp.LessOrEqual => VkCompareOp.LessOrEqual,
            CompareOp.Greater => VkCompareOp.Greater,
            CompareOp.NotEqual => VkCompareOp.NotEqual,
            CompareOp.GreaterOrEqual => VkCompareOp.GreaterOrEqual,
            CompareOp.Always => VkCompareOp.Always,
            _ => throw new ArgumentOutOfRangeException(nameof(compareOp), compareOp, null)
        };
    }

    private VkBorderColor ConvertBorderColor(BorderColor borderColor)
    {
        return borderColor switch
        {
            BorderColor.FloatTransparentBlack => VkBorderColor.FloatTransparentBlack,
            BorderColor.IntTransparentBlack => VkBorderColor.IntTransparentBlack,
            BorderColor.FloatOpaqueBlack => VkBorderColor.FloatOpaqueBlack,
            BorderColor.IntOpaqueBlack => VkBorderColor.IntOpaqueBlack,
            BorderColor.FloatOpaqueWhite => VkBorderColor.FloatOpaqueWhite,
            BorderColor.IntOpaqueWhite => VkBorderColor.IntOpaqueWhite,
            _ => throw new ArgumentOutOfRangeException(nameof(borderColor), borderColor, null)
        };
    }
}
