using System;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Windowing.Desktop;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VulkanTest;

public sealed unsafe class Swapchain : IDisposable
{
    public readonly VulkanDevice Device;

    [NotNull]
    public readonly GameWindow? Window = default!;

    public VkSwapchainKHR Handle;
    public VkExtent2D Extent { get; }
    public VkSurfaceFormatKHR SurfaceFormat;
    public int ImageCount => _images.Length;
    private VkImageView[] _imageViews;
    private VkImage[] _images;
    private readonly VkSurfaceKHR _surface;

    public Swapchain(VulkanDevice device, GameWindow? window, VkSurfaceKHR surface)
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

        var createInfo = new VkSwapchainCreateInfoKHR
        {
            surface = _surface,
            minImageCount = imageCount,
            imageFormat = SurfaceFormat.format,
            imageColorSpace = SurfaceFormat.colorSpace,
            imageExtent = Extent,
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.ColorAttachment,
            imageSharingMode = VkSharingMode.Exclusive,
            preTransform = swapChainSupport.Capabilities.currentTransform,
            compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
            presentMode = presentMode,
            clipped = true,
            oldSwapchain = VkSwapchainKHR.Null
        };

        vkCreateSwapchainKHR(Device.LogicalDevice, &createInfo, null, out Handle).CheckResult();

        GetImages();
        CreateImageViews();
    }

    private void GetImages()
    {
        ReadOnlySpan<VkImage> imagesSpan = vkGetSwapchainImagesKHR(Device.LogicalDevice, Handle);
        _images = imagesSpan.ToArray();
    }

    private void CreateImageViews()
    {
        _imageViews = new VkImageView[_images.Length];

        for (int i = 0; i < _images.Length; i++)
        {
            var viewCreateInfo = new VkImageViewCreateInfo(
                _images[i],
                VkImageViewType.Image2D,
                SurfaceFormat.format,
                VkComponentMapping.Rgba,
                new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1)
            );

            vkCreateImageView(Device.LogicalDevice, &viewCreateInfo, null, out _imageViews[i]).CheckResult();
        }
    }

    public VkImageView GetImageView(uint index)
    {
        return _imageViews[index];
    }

    public void Dispose()
    {
        for (int i = 0; i < _imageViews.Length; i++)
        {
            vkDestroyImageView(Device.LogicalDevice, _imageViews[i], null);
        }

        if (Handle != VkSwapchainKHR.Null)
        {
            vkDestroySwapchainKHR(Device.LogicalDevice, Handle, null);
        }
    }

    private ref struct SwapchainSupportDetails
    {
        public VkSurfaceCapabilitiesKHR Capabilities;
        public ReadOnlySpan<VkSurfaceFormatKHR> Formats;
        public ReadOnlySpan<VkPresentModeKHR> PresentModes;
    };

    private VkExtent2D ChooseSwapExtent(VkSurfaceCapabilitiesKHR capabilities)
    {
        if (capabilities.currentExtent.width > 0)
        {
            return capabilities.currentExtent;
        }
        else
        {
            VkExtent2D actualExtent = new VkExtent2D(Window.ClientSize.X, Window.ClientSize.Y);

            actualExtent = new VkExtent2D(
                Math.Max(capabilities.minImageExtent.width, Math.Min(capabilities.maxImageExtent.width, actualExtent.width)),
                Math.Max(capabilities.minImageExtent.height, Math.Min(capabilities.maxImageExtent.height, actualExtent.height))
            );

            return actualExtent;
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
}