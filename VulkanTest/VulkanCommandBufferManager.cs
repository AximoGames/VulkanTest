using System;
using System.Collections.Generic;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Vortice;

public unsafe class VulkanCommandBufferManager : IDisposable
{
    private readonly VulkanDevice _device;
    private readonly VulkanCommandPool _commandPool;
    private readonly List<VkCommandBuffer> _allocatedCommandBuffers = new List<VkCommandBuffer>();

    public VulkanCommandBufferManager(VulkanDevice device, VulkanCommandPool commandPool)
    {
        _device = device;
        _commandPool = commandPool;
    }

    public VkCommandBuffer AllocateCommandBuffer(VkCommandBufferLevel level = VkCommandBufferLevel.Primary)
    {
        VkCommandBuffer commandBuffer = _commandPool.AllocateCommandBuffer(level);
        _allocatedCommandBuffers.Add(commandBuffer);
        return commandBuffer;
    }

    public void BeginCommandBuffer(VkCommandBuffer commandBuffer, VkCommandBufferUsageFlags flags = VkCommandBufferUsageFlags.OneTimeSubmit)
    {
        VkCommandBufferBeginInfo beginInfo = new VkCommandBufferBeginInfo
        {
            flags = flags
        };
        vkBeginCommandBuffer(commandBuffer, &beginInfo).CheckResult();
    }

    public void EndCommandBuffer(VkCommandBuffer commandBuffer)
    {
        vkEndCommandBuffer(commandBuffer).CheckResult();
    }

    public void ResetCommandBuffer(VkCommandBuffer commandBuffer, VkCommandBufferResetFlags flags = VkCommandBufferResetFlags.None)
    {
        vkResetCommandBuffer(commandBuffer, flags).CheckResult();
    }

    public void FreeCommandBuffer(VkCommandBuffer commandBuffer)
    {
        _commandPool.FreeCommandBuffer(commandBuffer);
        _allocatedCommandBuffers.Remove(commandBuffer);
    }

    public void Dispose()
    {
        foreach (var commandBuffer in _allocatedCommandBuffers)
        {
            _commandPool.FreeCommandBuffer(commandBuffer);
        }
        _allocatedCommandBuffers.Clear();
    }
}