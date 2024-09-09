using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using OpenTK.Mathematics;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal sealed unsafe class VulkanSwapchainRenderTarget : VulkanRenderTarget
{
    public readonly VulkanDevice Device;

    [NotNull]
    public readonly Window? Window = default!;

    public VkSwapchainKHR VkSwapchain;
    public VkSurfaceFormatKHR SurfaceFormat;
    private readonly VkSurfaceKHR _surface;
    private VulkanImage[] _images;
    public override Vector2i Extent { get; }
    public override uint ImageCount { get; }

    public VulkanSwapchainRenderTarget(VulkanDevice device, Window? window, VkSurfaceKHR surface)
        : base(device)
    {
        Device = device;
        Window = window;
        _surface = surface;

        SwapchainSupportDetails swapChainSupport = QuerySwapchainSupport(device.PhysicalDevice, _surface);

        SurfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
        VkPresentModeKHR presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
        Extent = ChooseSwapExtent(swapChainSupport.Capabilities);

        uint imageCount = swapChainSupport.Capabilities.minImageCount + 1;
        if (swapChainSupport.Capabilities.maxImageCount > 0 &&
            imageCount > swapChainSupport.Capabilities.maxImageCount)
        {
            imageCount = swapChainSupport.Capabilities.maxImageCount;
        }
        ImageCount = imageCount;
        
        var createInfo = new VkSwapchainCreateInfoKHR
        {
            surface = _surface,
            minImageCount = imageCount,
            imageFormat = SurfaceFormat.format,
            imageColorSpace = SurfaceFormat.colorSpace,
            imageExtent = Extent.ToVkExtent2D(),
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.ColorAttachment,
            imageSharingMode = VkSharingMode.Exclusive,
            preTransform = swapChainSupport.Capabilities.currentTransform,
            compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
            presentMode = presentMode,
            clipped = true,
            oldSwapchain = VkSwapchainKHR.Null
        };

        vkCreateSwapchainKHR(Device.LogicalDevice, &createInfo, null, out VkSwapchain).CheckResult();

        var images = GetImages();
        var imageViews = new VkImageView[images.Length];
        for (int i = 0; i < images.Length; i++)
            imageViews[i] = CreateImageView(images[i]);

        var vulkanImages = new VulkanImage[images.Length];
        for (int i = 0; i < images.Length; i++)
            vulkanImages[i] = new VulkanImage(Device, Extent, images[i], imageViews[i], VkDeviceMemory.Null, SurfaceFormat.format, true);
        _images = vulkanImages;
    }

    private VkImage[] GetImages()
    {
        ReadOnlySpan<VkImage> imagesSpan = vkGetSwapchainImagesKHR(Device.LogicalDevice, VkSwapchain);
        return imagesSpan.ToArray();
    }

    private VkImageView CreateImageView(VkImage image)
    {
        var viewCreateInfo = new VkImageViewCreateInfo(
            image,
            VkImageViewType.Image2D,
            SurfaceFormat.format,
            VkComponentMapping.Rgba,
            new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1)
        );

        vkCreateImageView(Device.LogicalDevice, &viewCreateInfo, null, out var imageView).CheckResult();
        return imageView;
    }

    public override void Dispose()
    {
        for (int i = 0; i < _images.Length; i++)
            vkDestroyImageView(Device.LogicalDevice, _images[i].ImageView, null);

        if (VkSwapchain != VkSwapchainKHR.Null)
            vkDestroySwapchainKHR(Device.LogicalDevice, VkSwapchain, null);
    }

    private ref struct SwapchainSupportDetails
    {
        public VkSurfaceCapabilitiesKHR Capabilities;
        public ReadOnlySpan<VkSurfaceFormatKHR> Formats;
        public ReadOnlySpan<VkPresentModeKHR> PresentModes;
    };

    private Vector2i ChooseSwapExtent(VkSurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.currentExtent.width > 0)
        {
            return capabilities.currentExtent.ToVector2i();
        }
        else
        {
            VkExtent2D actualExtent = new VkExtent2D(Window.ClientSize.X, Window.ClientSize.Y);

            actualExtent = new VkExtent2D(
                Math.Max(capabilities.minImageExtent.width, Math.Min(capabilities.maxImageExtent.width, actualExtent.width)),
                Math.Max(capabilities.minImageExtent.height, Math.Min(capabilities.maxImageExtent.height, actualExtent.height))
            );

            return actualExtent.ToVector2i();
        }
    }

    private static SwapchainSupportDetails QuerySwapchainSupport(VkPhysicalDevice device, VkSurfaceKHR surface)
    {
        SwapchainSupportDetails details = new SwapchainSupportDetails();
        vkGetPhysicalDeviceSurfaceCapabilitiesKHR(device, surface, out details.Capabilities).CheckResult();

        details.Formats = vkGetPhysicalDeviceSurfaceFormatsKHR(device, surface);
        details.PresentModes = vkGetPhysicalDeviceSurfacePresentModesKHR(device, surface);
        return details;
    }

    private static VkSurfaceFormatKHR ChooseSwapSurfaceFormat(ReadOnlySpan<VkSurfaceFormatKHR> availableFormats)
    {
        // If the surface format list only includes one entry with VK_FORMAT_UNDEFINED,
        // there is no preferred format, so we assume VK_FORMAT_B8G8R8A8_UNORM
        if ((availableFormats.Length == 1) && (availableFormats[0].format == VkFormat.Undefined))
        {
            return new VkSurfaceFormatKHR(VkFormat.B8G8R8A8Unorm, availableFormats[0].colorSpace);
        }

        // iterate over the list of available surface format and
        // check for the presence of VK_FORMAT_B8G8R8A8_UNORM
        foreach (VkSurfaceFormatKHR availableFormat in availableFormats)
        {
            if (availableFormat.format == VkFormat.B8G8R8A8Unorm)
            {
                return availableFormat;
            }
        }

        return availableFormats[0];
    }

    private static VkPresentModeKHR ChooseSwapPresentMode(ReadOnlySpan<VkPresentModeKHR> availablePresentModes)
    {
        foreach (VkPresentModeKHR availablePresentMode in availablePresentModes)
        {
            if (availablePresentMode == VkPresentModeKHR.Mailbox)
            {
                return availablePresentMode;
            }
        }

        return VkPresentModeKHR.Fifo;
    }
    
    public override VulkanImage GetImage(uint index)
        => _images[index];
}