using System;
using System.Collections.Generic;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanDevice : IDisposable
{
    public VkPhysicalDevice PhysicalDevice;
    public VkDevice LogicalDevice;
    public VkQueue GraphicsQueue;
    public VkQueue PresentQueue;

    public (uint GraphicsFamily, uint PresentFamily) QueueFamilies { get; private set; }

    private readonly VulkanInstance _instance;
    private readonly VkSurfaceKHR _surface;

    public VulkanDevice(VulkanInstance instance, VkSurfaceKHR surface)
    {
        _instance = instance;
        _surface = surface;
        CreateDevice();
    }

    private void CreateDevice()
    {
        PickPhysicalDevice();
        CreateLogicalDevice();
    }

    private void PickPhysicalDevice()
    {
        var physicalDevices = vkEnumeratePhysicalDevices(_instance.Instance);
        PhysicalDevice = physicalDevices[0]; // For simplicity, we're just picking the first device

        vkGetPhysicalDeviceProperties(PhysicalDevice, out VkPhysicalDeviceProperties properties);
        QueueFamilies = FindQueueFamilies(PhysicalDevice, _surface);

        Log.Info($"Selected physical device: {properties.GetDeviceName()}");
    }

    private void CreateLogicalDevice()
    {
        float priority = 1.0f;
        VkDeviceQueueCreateInfo queueCreateInfo = new VkDeviceQueueCreateInfo
        {
            queueFamilyIndex = QueueFamilies.GraphicsFamily,
            queueCount = 1,
            pQueuePriorities = &priority
        };

        List<string> enabledExtensions = new List<string>
        {
            VK_KHR_SWAPCHAIN_EXTENSION_NAME.GetStringFromUtf8Buffer()
        };

        VkPhysicalDeviceFeatures deviceFeatures = new VkPhysicalDeviceFeatures();

        using var deviceExtensionNames = new VkStringArray(enabledExtensions);

        var deviceCreateInfo = new VkDeviceCreateInfo
        {
            queueCreateInfoCount = 1,
            pQueueCreateInfos = &queueCreateInfo,
            enabledExtensionCount = deviceExtensionNames.Length,
            ppEnabledExtensionNames = deviceExtensionNames,
            pEnabledFeatures = &deviceFeatures,
        };

        vkCreateDevice(PhysicalDevice, &deviceCreateInfo, null, out LogicalDevice)
            .CheckResult("Failed to create Vulkan Logical Device");

        vkGetDeviceQueue(LogicalDevice, QueueFamilies.GraphicsFamily, 0, out GraphicsQueue);
        vkGetDeviceQueue(LogicalDevice, QueueFamilies.PresentFamily, 0, out PresentQueue);

        Log.Info("Logical device created successfully");
    }

    private static (uint GraphicsFamily, uint PresentFamily) FindQueueFamilies(VkPhysicalDevice device, VkSurfaceKHR surface)
    {
        ReadOnlySpan<VkQueueFamilyProperties> queueFamilies = vkGetPhysicalDeviceQueueFamilyProperties(device);

        uint graphicsFamily = VK_QUEUE_FAMILY_IGNORED;
        uint presentFamily = VK_QUEUE_FAMILY_IGNORED;
        uint i = 0;
        foreach (VkQueueFamilyProperties queueFamily in queueFamilies)
        {
            if ((queueFamily.queueFlags & VkQueueFlags.Graphics) != VkQueueFlags.None)
            {
                graphicsFamily = i;
            }

            vkGetPhysicalDeviceSurfaceSupportKHR(device, i, surface, out VkBool32 presentSupport);
            if (presentSupport)
            {
                presentFamily = i;
            }

            if (graphicsFamily != VK_QUEUE_FAMILY_IGNORED
                && presentFamily != VK_QUEUE_FAMILY_IGNORED)
            {
                break;
            }

            i++;
        }

        return (graphicsFamily, presentFamily);
    }

    public void Dispose()
    {
        if (LogicalDevice != VkDevice.Null)
        {
            vkDestroyDevice(LogicalDevice, null);
        }
    }
}