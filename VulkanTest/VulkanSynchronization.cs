using System;
using System.Collections.Generic;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VulkanTest;

public unsafe class VulkanSynchronization : IDisposable
{
    private readonly VulkanDevice _device;
    private readonly List<VkSemaphore> _recycledSemaphores = new List<VkSemaphore>();

    public VulkanSynchronization(VulkanDevice device)
    {
        _device = device;
    }

    public VkSemaphore CreateSemaphore()
    {
        VkSemaphore semaphore;
        vkCreateSemaphore(_device.LogicalDevice, out semaphore).CheckResult();
        return semaphore;
    }

    public VkFence CreateFence(VkFenceCreateFlags flags = VkFenceCreateFlags.Signaled)
    {
        VkFenceCreateInfo fenceCreateInfo = new VkFenceCreateInfo(flags);
        VkFence fence;
        vkCreateFence(_device.LogicalDevice, &fenceCreateInfo, null, out fence).CheckResult();
        return fence;
    }

    public VkSemaphore AcquireSemaphore()
    {
        if (_recycledSemaphores.Count == 0)
        {
            return CreateSemaphore();
        }
        else
        {
            VkSemaphore semaphore = _recycledSemaphores[_recycledSemaphores.Count - 1];
            _recycledSemaphores.RemoveAt(_recycledSemaphores.Count - 1);
            return semaphore;
        }
    }

    public void RecycleSemaphore(VkSemaphore semaphore)
    {
        if (semaphore != VkSemaphore.Null)
        {
            _recycledSemaphores.Add(semaphore);
        }
    }

    public void WaitForFence(VkFence fence)
    {
        vkWaitForFences(_device.LogicalDevice, fence, true, ulong.MaxValue);
    }

    public void ResetFence(VkFence fence)
    {
        vkResetFences(_device.LogicalDevice, fence);
    }

    public void Dispose()
    {
        foreach (VkSemaphore semaphore in _recycledSemaphores)
        {
            vkDestroySemaphore(_device.LogicalDevice, semaphore, null);
        }
        _recycledSemaphores.Clear();
    }
}