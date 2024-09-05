using System;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanCommandPool : IDisposable
{
    private readonly VulkanDevice _device;
    public VkCommandPool Handle;

    public VulkanCommandPool(VulkanDevice device, VkCommandPoolCreateFlags flags = VkCommandPoolCreateFlags.Transient)
    {
        _device = device;
        CreateCommandPool(flags);
    }

    private void CreateCommandPool(VkCommandPoolCreateFlags flags)
    {
        VkCommandPoolCreateInfo poolCreateInfo = new VkCommandPoolCreateInfo
        {
            flags = flags,
            queueFamilyIndex = _device.QueueFamilies.GraphicsFamily,
        };
        VkCommandPool vkCommandPool;
        vkCreateCommandPool(_device.LogicalDevice, &poolCreateInfo, null, &vkCommandPool).CheckResult();
        Handle = vkCommandPool;
    }

    public VkCommandBuffer AllocateCommandBuffer(VkCommandBufferLevel level = VkCommandBufferLevel.Primary)
    {
        VkCommandBufferAllocateInfo allocInfo = new VkCommandBufferAllocateInfo
        {
            commandPool = Handle,
            level = level,
            commandBufferCount = 1
        };

        VkCommandBuffer commandBuffer;
        vkAllocateCommandBuffers(_device.LogicalDevice, &allocInfo, &commandBuffer).CheckResult();
        return commandBuffer;
    }

    public void FreeCommandBuffer(VkCommandBuffer commandBuffer)
    {
        vkFreeCommandBuffers(_device.LogicalDevice, Handle, 1, &commandBuffer);
    }

    public void Reset()
    {
        vkResetCommandPool(_device.LogicalDevice, Handle, VkCommandPoolResetFlags.None);
    }

    public void Dispose()
    {
        if (Handle != VkCommandPool.Null)
        {
            vkDestroyCommandPool(_device.LogicalDevice, Handle, null);
            Handle = VkCommandPool.Null;
        }
    }
}