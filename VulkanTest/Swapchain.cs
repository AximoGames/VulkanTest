using System;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Windowing.Desktop;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Vortice;

public sealed unsafe class Swapchain : IDisposable
{
    public readonly GraphicsDevice Device;

    [NotNull]
    public readonly GameWindow? Window = default!;

    public VkSwapchainKHR Handle;
    public VkExtent2D Extent { get; }

    public VkSurfaceFormatKHR SurfaceFormat;

    private VkImageView[] _imageViews;

    public Swapchain(GraphicsDevice device, GameWindow? window)
    {
        Device = device;
        Window = window;

        SwapChainSupportDetails swapChainSupport = QuerySwapChainSupport(device.PhysicalDevice, device._surface);

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
            surface = device._surface,
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

        vkCreateSwapchainKHR(device.VkDevice, &createInfo, null, out Handle).CheckResult();

        CreateImageViews();
    }

    private void CreateImageViews()
    {
        var swapChainImages = vkGetSwapchainImagesKHR(Device.VkDevice, Handle);
        _imageViews = new VkImageView[swapChainImages.Length];

        for (int i = 0; i < swapChainImages.Length; i++)
        {
            var viewCreateInfo = new VkImageViewCreateInfo(
                swapChainImages[i],
                VkImageViewType.Image2D,
                SurfaceFormat.format,
                VkComponentMapping.Rgba,
                new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1)
            );

            vkCreateImageView(Device.VkDevice, &viewCreateInfo, null, out _imageViews[i]).CheckResult();
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
            vkDestroyImageView(Device.VkDevice, _imageViews[i], null);
        }

        if (Handle != VkSwapchainKHR.Null)
        {
            vkDestroySwapchainKHR(Device.VkDevice, Handle, null);
        }
    }

    private ref struct SwapChainSupportDetails
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

    private static SwapChainSupportDetails QuerySwapChainSupport(VkPhysicalDevice device, VkSurfaceKHR surface)
    {
        SwapChainSupportDetails details = new SwapChainSupportDetails();
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