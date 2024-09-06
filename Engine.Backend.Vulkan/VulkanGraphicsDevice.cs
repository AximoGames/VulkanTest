﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using System.Collections.Generic;
using System.Text;
using Vortice.ShaderCompiler;
using OpenTK.Mathematics;

namespace Engine.Vulkan;

internal unsafe sealed class VulkanBackendGraphicsDevice : BackendGraphicsDevice
{
    private static readonly string _engineName = "Vortice";

    public readonly VulkanInstance VulkanInstance;
    public readonly VulkanDevice VulkanDevice;

    private readonly VkSurfaceKHR _surface;
    public readonly VulkanSwapchain Swapchain;
    public VulkanPipeline Pipeline;
    private PerFrame[] _perFrameData; // TODO: Pin during init?
    internal readonly VulkanBufferManager VulkanBufferManager;
    public VulkanCommandPool CommandPool;
    public VulkanSynchronization Synchronization;

    public uint CurrentSwapchainImageIndex;

    public VulkanShaderManager ShaderManager { get; private set; }

    public VulkanCommandBufferManager CommandBufferManager;

    public VkClearColorValue? ClearColor { get; set; }

    public VulkanBackendGraphicsDevice(string applicationName, bool enableValidation, Window window)
    {
        // Need to initialize
        vkInitialize().CheckResult();

        VulkanInstance = new VulkanInstance(applicationName, enableValidation, window.WindowManager);

        _surface = CreateSurface(window);

        VulkanDevice = new VulkanDevice(VulkanInstance, _surface);

        ShaderManager = new VulkanShaderManager(VulkanDevice);

        // Create swap chain
        Swapchain = new VulkanSwapchain(VulkanDevice, window, _surface);

        // Initialize _perFrame array
        _perFrameData = new PerFrame[Swapchain.ImageCount];

        CommandPool = new VulkanCommandPool(VulkanDevice);
        Synchronization = new VulkanSynchronization(VulkanDevice);

        VulkanBufferManager = new VulkanBufferManager(VulkanDevice, CommandPool);

        CommandBufferManager = new VulkanCommandBufferManager(VulkanDevice, CommandPool);

        for (var i = 0; i < _perFrameData.Length; i++)
        {
            _perFrameData[i].QueueSubmitFence = Synchronization.CreateFence();

            _perFrameData[i].PrimaryCommandPool = new VulkanCommandPool(VulkanDevice);
            _perFrameData[i].PrimaryCommandBuffer = _perFrameData[i].PrimaryCommandPool.AllocateCommandBuffer();
        }
    }

    public override void Dispose()
    {
        // Don't release anything until the GPU is completely idle.
        vkDeviceWaitIdle(VulkanDevice.LogicalDevice);

        VulkanBufferManager.Dispose();

        Swapchain.Dispose();

        for (var i = 0; i < _perFrameData.Length; i++)
        {
            vkDestroyFence(VulkanDevice.LogicalDevice, _perFrameData[i].QueueSubmitFence, null);

            if (_perFrameData[i].PrimaryCommandBuffer != IntPtr.Zero)
            {
                _perFrameData[i].PrimaryCommandPool.FreeCommandBuffer(_perFrameData[i].PrimaryCommandBuffer);
                _perFrameData[i].PrimaryCommandBuffer = IntPtr.Zero;
            }

            _perFrameData[i].PrimaryCommandPool.Dispose();

            if (_perFrameData[i].SwapchainAcquireSemaphore != VkSemaphore.Null)
            {
                vkDestroySemaphore(VulkanDevice.LogicalDevice, _perFrameData[i].SwapchainAcquireSemaphore, null);
                _perFrameData[i].SwapchainAcquireSemaphore = VkSemaphore.Null;
            }

            if (_perFrameData[i].SwapchainReleaseSemaphore != VkSemaphore.Null)
            {
                vkDestroySemaphore(VulkanDevice.LogicalDevice, _perFrameData[i].SwapchainReleaseSemaphore, null);
                _perFrameData[i].SwapchainReleaseSemaphore = VkSemaphore.Null;
            }
        }

        Synchronization.Dispose();

        Pipeline.Dispose();

        CommandBufferManager.Dispose();

        CommandPool.Dispose();

        VulkanDevice.Dispose();

        if (_surface != VkSurfaceKHR.Null)
        {
            vkDestroySurfaceKHR(VulkanInstance.Instance, _surface, null);
        }

        VulkanInstance.Dispose();
    }

    public override BackendPipelineBuilder CreatePipelineBuilder()
    {
        return new VulkanBackendPipelineBuilder(VulkanDevice, Swapchain, ShaderManager, VulkanBufferManager);
    }
    
    public override void InitializePipeline(Action<BackendPipelineBuilder> callback)
    {
        var builder = CreatePipelineBuilder();
        callback(builder);
        Pipeline = (VulkanPipeline)builder.Build();
    }

    public override void RenderFrame(Action<BackendRenderContext> draw, [CallerMemberName] string? frameName = null)
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
        VkCommandBuffer cmd = _perFrameData[CurrentSwapchainImageIndex].PrimaryCommandBuffer;

        var renderContext = new VulkanBackendRenderContext(VulkanDevice, cmd, Swapchain.Extent);

        CommandBufferManager.BeginCommandBuffer(cmd);

        BeginRenderPass(cmd, Swapchain.Extent);
        draw(renderContext);
        EndRenderPass(cmd);

        CommandBufferManager.EndCommandBuffer(cmd);

        if (_perFrameData[CurrentSwapchainImageIndex].SwapchainReleaseSemaphore == VkSemaphore.Null)
        {
            _perFrameData[CurrentSwapchainImageIndex].SwapchainReleaseSemaphore = Synchronization.CreateSemaphore();
        }

        VkPipelineStageFlags wait_stage = VkPipelineStageFlags.ColorAttachmentOutput;
        VkSemaphore waitSemaphore = _perFrameData[CurrentSwapchainImageIndex].SwapchainAcquireSemaphore;
        VkSemaphore signalSemaphore = _perFrameData[CurrentSwapchainImageIndex].SwapchainReleaseSemaphore;

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
        vkQueueSubmit(VulkanDevice.GraphicsQueue, submitInfo, _perFrameData[CurrentSwapchainImageIndex].QueueSubmitFence);

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
        VkSemaphore acquireSemaphore = Synchronization.AcquireSemaphore();

        VkResult result = vkAcquireNextImageKHR(VulkanDevice.LogicalDevice, Swapchain.Handle, ulong.MaxValue, acquireSemaphore, VkFence.Null, out imageIndex);

        if (result != VkResult.Success)
        {
            Synchronization.RecycleSemaphore(acquireSemaphore);
            return result;
        }

        if (_perFrameData[imageIndex].QueueSubmitFence != VkFence.Null)
        {
            Synchronization.WaitForFence(_perFrameData[imageIndex].QueueSubmitFence);
            Synchronization.ResetFence(_perFrameData[imageIndex].QueueSubmitFence);
        }

        if (_perFrameData[imageIndex].PrimaryCommandPool.Handle != VkCommandPool.Null)
        {
            _perFrameData[imageIndex].PrimaryCommandPool.Reset();
        }

        // Recycle the old semaphore back into the semaphore manager.
        Synchronization.RecycleSemaphore(_perFrameData[imageIndex].SwapchainAcquireSemaphore);

        _perFrameData[imageIndex].SwapchainAcquireSemaphore = acquireSemaphore;

        return VkResult.Success;
    }

    private VkResult PresentImage(uint imageIndex)
    {
        return vkQueuePresentKHR(VulkanDevice.PresentQueue, _perFrameData[imageIndex].SwapchainReleaseSemaphore, Swapchain.Handle, imageIndex);
    }

    public static implicit operator VkDevice(VulkanBackendGraphicsDevice device) => device.VulkanDevice.LogicalDevice;

    #region Private Methods

    private VkSurfaceKHR CreateSurface(Window window)
    {
        // GLFW.CreateWindowSurface(new VkHandle(VulkanInstance.Instance.Handle), window.WindowPtr, null, out var handle);
        var handle = window.CreateVulkanSurfaceHandle(VulkanInstance.Instance.Handle);
        return new VkSurfaceKHR((ulong)handle);
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

    public void BeginRenderPass(VkCommandBuffer commandBuffer, Vector2i size)
    {
        VkRenderingAttachmentInfo colorAttachmentInfo = new VkRenderingAttachmentInfo
        {
            imageView = Swapchain.GetImageView(CurrentSwapchainImageIndex),
            imageLayout = VkImageLayout.ColorAttachmentOptimal,
            loadOp = VkAttachmentLoadOp.Load,
            storeOp = VkAttachmentStoreOp.Store,
        };

        if (ClearColor.HasValue)
        {
            colorAttachmentInfo.loadOp = VkAttachmentLoadOp.Clear;
            colorAttachmentInfo.clearValue = new VkClearValue { color = ClearColor.Value };
        }

        VkRenderingInfo renderingInfo = new VkRenderingInfo
        {
            renderArea = new VkRect2D(VkOffset2D.Zero, size.ToVkExtent2D()),
            layerCount = 1,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachmentInfo
        };

        vkCmdBeginRendering(commandBuffer, &renderingInfo);
        vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, Pipeline.PipelineHandle);
    }

    public void EndRenderPass(VkCommandBuffer commandBuffer)
    {
        vkCmdEndRendering(commandBuffer);
    }
}