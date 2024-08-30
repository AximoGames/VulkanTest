using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using System.Collections.Generic;
using System.Text;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using Vortice.ShaderCompiler;
using OpenTK.Mathematics;

namespace Vortice;

public unsafe sealed class GraphicsDevice : IDisposable
{
    private static readonly string s_EngineName = "Vortice";

    public readonly VulkanInstance VulkanInstance;
    public readonly VulkanDevice VulkanDevice;

    internal readonly VkSurfaceKHR _surface;
    public readonly Swapchain Swapchain;
    public readonly VulkanPipeline Pipeline;
    private PerFrame[] _perFrame; // TODO: Pin during init?
    public readonly BufferManager BufferManager;
    public VulkanCommandPool CommandPool;

    private readonly List<VkSemaphore> _recycledSemaphores = new List<VkSemaphore>();

    public uint CurrentSwapchainImageIndex;

    public ShaderManager ShaderManager { get; private set; }

    public GraphicsDevice(string applicationName, bool enableValidation, GameWindow window)
    {
        VulkanInstance = new VulkanInstance(applicationName, enableValidation);

        _surface = CreateSurface(window);

        VulkanDevice = new VulkanDevice(VulkanInstance, _surface);

        ShaderManager = new ShaderManager(VulkanDevice.LogicalDevice);

        // Create swap chain
        Swapchain = new Swapchain(VulkanDevice, window, _surface);

        // Initialize _perFrame array
        _perFrame = new PerFrame[Swapchain.ImageCount];

        Pipeline = new VulkanPipeline(VulkanDevice, Swapchain, ShaderManager);

        CommandPool = new VulkanCommandPool(VulkanDevice);

        BufferManager = new BufferManager(VulkanDevice, CommandPool);
        BufferManager.CreateVertexBuffer(Vertices);
        BufferManager.CreateIndexBuffer(Indices);

        for (var i = 0; i < _perFrame.Length; i++)
        {
            VkFenceCreateInfo fenceCreateInfo = new VkFenceCreateInfo(VkFenceCreateFlags.Signaled);
            vkCreateFence(VulkanDevice.LogicalDevice, &fenceCreateInfo, null, out _perFrame[i].QueueSubmitFence).CheckResult();

            _perFrame[i].PrimaryCommandPool = new VulkanCommandPool(VulkanDevice);
            _perFrame[i].PrimaryCommandBuffer = _perFrame[i].PrimaryCommandPool.AllocateCommandBuffer();
        }
    }

    public Vertex[] Vertices = new Vertex[]
    {
        new Vertex { pos = new Vector2(-0.5f, -0.5f), color = new Vector3(1.0f, 0.0f, 0.0f) },
        new Vertex { pos = new Vector2(0.5f, -0.5f), color = new Vector3(1.0f, 0.0f, 0.0f) },
        new Vertex { pos = new Vector2(0.5f, 0.5f), color = new Vector3(0.0f, 1.0f, 0.0f) },
        new Vertex { pos = new Vector2(-0.5f, 0.5f), color = new Vector3(0.0f, 0.0f, 1.0f) }
    };

    public ushort[] Indices =
    {
        0, 1, 2, 2, 3, 0,
    };

    public void Dispose()
    {
        // Don't release anything until the GPU is completely idle.
        vkDeviceWaitIdle(VulkanDevice.LogicalDevice);

        BufferManager.Dispose();

        Swapchain.Dispose();

        for (var i = 0; i < _perFrame.Length; i++)
        {
            vkDestroyFence(VulkanDevice.LogicalDevice, _perFrame[i].QueueSubmitFence, null);

            if (_perFrame[i].PrimaryCommandBuffer != IntPtr.Zero)
            {
                _perFrame[i].PrimaryCommandPool.FreeCommandBuffer(_perFrame[i].PrimaryCommandBuffer);
                _perFrame[i].PrimaryCommandBuffer = IntPtr.Zero;
            }

            _perFrame[i].PrimaryCommandPool.Dispose();

            if (_perFrame[i].SwapchainAcquireSemaphore != VkSemaphore.Null)
            {
                vkDestroySemaphore(VulkanDevice.LogicalDevice, _perFrame[i].SwapchainAcquireSemaphore, null);
                _perFrame[i].SwapchainAcquireSemaphore = VkSemaphore.Null;
            }

            if (_perFrame[i].SwapchainReleaseSemaphore != VkSemaphore.Null)
            {
                vkDestroySemaphore(VulkanDevice.LogicalDevice, _perFrame[i].SwapchainReleaseSemaphore, null);
                _perFrame[i].SwapchainReleaseSemaphore = VkSemaphore.Null;
            }
        }

        foreach (VkSemaphore semaphore in _recycledSemaphores)
        {
            vkDestroySemaphore(VulkanDevice.LogicalDevice, semaphore, null);
        }

        _recycledSemaphores.Clear();

        Pipeline.Dispose();

        CommandPool.Dispose();

        VulkanDevice.Dispose();

        if (_surface != VkSurfaceKHR.Null)
        {
            vkDestroySurfaceKHR(VulkanInstance.Instance, _surface, null);
        }

        VulkanInstance.Dispose();
    }

    public void RenderFrame(Action<VkCommandBuffer, VkExtent2D> draw, [CallerMemberName] string? frameName = null)
    {
        VkResult result = AcquireNextImage(out CurrentSwapchainImageIndex);

        // Handle outdated error in acquire.
        if (result == VkResult.SuboptimalKHR || result == VkResult.ErrorOutOfDateKHR)
        {
            result = AcquireNextImage(out CurrentSwapchainImageIndex);
        }

        if (result != VkResult.Success)
        {
            vkDeviceWaitIdle(VulkanDevice.LogicalDevice);
            return;
        }

        // Begin command recording
        VkCommandBuffer cmd = _perFrame[CurrentSwapchainImageIndex].PrimaryCommandBuffer;

        VkCommandBufferBeginInfo beginInfo = new VkCommandBufferBeginInfo
        {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit
        };
        vkBeginCommandBuffer(cmd, &beginInfo).CheckResult();

        draw(cmd, Swapchain.Extent);

        // Complete the command buffer.
        vkEndCommandBuffer(cmd).CheckResult();

        if (_perFrame[CurrentSwapchainImageIndex].SwapchainReleaseSemaphore == VkSemaphore.Null)
        {
            vkCreateSemaphore(VulkanDevice.LogicalDevice, out _perFrame[CurrentSwapchainImageIndex].SwapchainReleaseSemaphore).CheckResult();
        }

        VkPipelineStageFlags wait_stage = VkPipelineStageFlags.ColorAttachmentOutput;
        VkSemaphore waitSemaphore = _perFrame[CurrentSwapchainImageIndex].SwapchainAcquireSemaphore;
        VkSemaphore signalSemaphore = _perFrame[CurrentSwapchainImageIndex].SwapchainReleaseSemaphore;

        VkSubmitInfo submitInfo = new VkSubmitInfo
        {
            commandBufferCount = 1u,
            pCommandBuffers = &cmd,
            waitSemaphoreCount = 1u,
            pWaitSemaphores = &waitSemaphore,
            pWaitDstStageMask = &wait_stage,
            signalSemaphoreCount = 1u,
            pSignalSemaphores = &signalSemaphore
        };

        // Submit command buffer to graphics queue
        vkQueueSubmit(VulkanDevice.GraphicsQueue, submitInfo, _perFrame[CurrentSwapchainImageIndex].QueueSubmitFence);

        result = PresentImage(CurrentSwapchainImageIndex);

        // Handle Outdated error in present.
        if (result == VkResult.SuboptimalKHR || result == VkResult.ErrorOutOfDateKHR)
        {
            // Handle resize if needed
        }
        else if (result != VkResult.Success)
        {
            Log.Error("Failed to present swapchain image.");
        }
    }

    private VkResult AcquireNextImage(out uint imageIndex)
    {
        VkSemaphore acquireSemaphore;
        if (_recycledSemaphores.Count == 0)
        {
            vkCreateSemaphore(VulkanDevice.LogicalDevice, out acquireSemaphore).CheckResult();
        }
        else
        {
            acquireSemaphore = _recycledSemaphores[_recycledSemaphores.Count - 1];
            _recycledSemaphores.RemoveAt(_recycledSemaphores.Count - 1);
        }

        VkResult result = vkAcquireNextImageKHR(VulkanDevice.LogicalDevice, Swapchain.Handle, ulong.MaxValue, acquireSemaphore, VkFence.Null, out imageIndex);

        if (result != VkResult.Success)
        {
            _recycledSemaphores.Add(acquireSemaphore);
            return result;
        }

        if (_perFrame[imageIndex].QueueSubmitFence != VkFence.Null)
        {
            vkWaitForFences(VulkanDevice.LogicalDevice, _perFrame[imageIndex].QueueSubmitFence, true, ulong.MaxValue);
            vkResetFences(VulkanDevice.LogicalDevice, _perFrame[imageIndex].QueueSubmitFence);
        }

        if (_perFrame[imageIndex].PrimaryCommandPool.Handle != VkCommandPool.Null)
        {
            _perFrame[imageIndex].PrimaryCommandPool.Reset();
        }

        // Recycle the old semaphore back into the semaphore manager.
        VkSemaphore old_semaphore = _perFrame[imageIndex].SwapchainAcquireSemaphore;

        if (old_semaphore != VkSemaphore.Null)
        {
            _recycledSemaphores.Add(old_semaphore);
        }

        _perFrame[imageIndex].SwapchainAcquireSemaphore = acquireSemaphore;

        return VkResult.Success;
    }

    private VkResult PresentImage(uint imageIndex)
    {
        return vkQueuePresentKHR(VulkanDevice.PresentQueue, _perFrame[imageIndex].SwapchainReleaseSemaphore, Swapchain.Handle, imageIndex);
    }

    public static implicit operator VkDevice(GraphicsDevice device) => device.VulkanDevice.LogicalDevice;

    #region Private Methods

    private VkSurfaceKHR CreateSurface(GameWindow window)
    {
        GLFW.CreateWindowSurface(new VkHandle(VulkanInstance.Instance.Handle), window.WindowPtr, null, out var handle);
        return new VkSurfaceKHR((ulong)handle.Handle);
    }

    #endregion

    private struct PerFrame
    {
        public VkImage Image;
        public VkImageView ImageView;
        public VkFence QueueSubmitFence;
        public VulkanCommandPool PrimaryCommandPool;
        public VkCommandBuffer PrimaryCommandBuffer;
        public VkSemaphore SwapchainAcquireSemaphore;
        public VkSemaphore SwapchainReleaseSemaphore;
    }
}