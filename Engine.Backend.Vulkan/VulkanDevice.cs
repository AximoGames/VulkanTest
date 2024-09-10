using System;
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

internal unsafe sealed class VulkanDevice : BackendDevice
{
    private static readonly string _engineName = "Vortice";

    public readonly VulkanInstance VulkanInstance;
    public VkPhysicalDevice PhysicalDevice;
    public VkDevice LogicalDevice;
    public VkQueue GraphicsQueue;
    public VkQueue PresentQueue;

    public (uint GraphicsFamily, uint PresentFamily) QueueFamilies { get; private set; }

    private readonly VkSurfaceKHR _surface;
    public readonly VulkanSwapchainRenderTarget SwapchainRenderTarget;
    private PerFrame[] _perFrameData;
    internal readonly VulkanBufferManager VulkanBufferManager;
    internal readonly VulkanImageManager VulkanImageManager;
    public VulkanCommandPool CommandPool;
    public VulkanSynchronization Synchronization;

    public uint CurrentSwapchainImageIndex;


    public VulkanShaderManager ShaderManager { get; private set; }
    public VulkanCommandBufferManager CommandBufferManager;
    public VulkanDescriptorSetManager DescriptorSetManager { get; private set; }

    public VkClearColorValue? ClearColor { get; set; }

    public VulkanDevice(string applicationName, bool enableValidation, Window window, IEnumerable<string>? suppressDebugMessages)
    {
        // Need to initialize
        vkInitialize().CheckResult();

        VulkanInstance = new VulkanInstance(applicationName, enableValidation, window.WindowManager, suppressDebugMessages);

        _surface = CreateSurface(window);

        CreateDevice();

        ShaderManager = new VulkanShaderManager(this);

        // Create swap chain
        SwapchainRenderTarget = new VulkanSwapchainRenderTarget(this, window, _surface);

        // Initialize _perFrame array
        _perFrameData = new PerFrame[SwapchainRenderTarget.ImageCount];

        CommandPool = new VulkanCommandPool(this);
        Synchronization = new VulkanSynchronization(this);

        VulkanBufferManager = new VulkanBufferManager(this, CommandPool);

        CommandBufferManager = new VulkanCommandBufferManager(this, CommandPool);

        VulkanImageManager = new VulkanImageManager(this, VulkanBufferManager);

        DescriptorSetManager = new VulkanDescriptorSetManager(this, 1000); // Adjust the number as needed

        for (var i = 0; i < _perFrameData.Length; i++)
        {
            _perFrameData[i].QueueSubmitFence = Synchronization.CreateFence();

            _perFrameData[i].PrimaryCommandPool = new VulkanCommandPool(this);
            _perFrameData[i].PrimaryCommandBuffer = _perFrameData[i].PrimaryCommandPool.AllocateCommandBuffer();
        }
    }

    private void CreateDevice()
    {
        PickPhysicalDevice();
        CreateLogicalDevice();
    }

    private void PickPhysicalDevice()
    {
        var physicalDevices = vkEnumeratePhysicalDevices(VulkanInstance.Instance);
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
            VK_KHR_SWAPCHAIN_EXTENSION_NAME.GetStringFromUtf8Buffer(),
            VK_KHR_DYNAMIC_RENDERING_EXTENSION_NAME.GetStringFromUtf8Buffer()
        };

        VkPhysicalDeviceFeatures deviceFeatures = new VkPhysicalDeviceFeatures();

        VkPhysicalDeviceVulkan12Features vulkan12Features = new VkPhysicalDeviceVulkan12Features
        {
            descriptorIndexing = true,
            descriptorBindingPartiallyBound = true,
            runtimeDescriptorArray = true,
            descriptorBindingVariableDescriptorCount = true,
            descriptorBindingUpdateUnusedWhilePending = true,
            // descriptorBindingUniformBufferUpdateAfterBind = true,
        };
        
        VkPhysicalDeviceDynamicRenderingFeatures dynamicRenderingFeatures = new VkPhysicalDeviceDynamicRenderingFeatures
        {
            pNext = &vulkan12Features,
            dynamicRendering = true
        };

        using var deviceExtensionNames = new VkStringArray(enabledExtensions);

        var deviceCreateInfo = new VkDeviceCreateInfo
        {
            pNext = &dynamicRenderingFeatures,
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

    public override void Dispose()
    {
        // Don't release anything until the GPU is completely idle.
        vkDeviceWaitIdle(LogicalDevice);

        VulkanBufferManager.Dispose();

        SwapchainRenderTarget.Dispose();

        for (var i = 0; i < _perFrameData.Length; i++)
        {
            vkDestroyFence(LogicalDevice, _perFrameData[i].QueueSubmitFence, null);

            if (_perFrameData[i].PrimaryCommandBuffer != IntPtr.Zero)
            {
                _perFrameData[i].PrimaryCommandPool.FreeCommandBuffer(_perFrameData[i].PrimaryCommandBuffer);
                _perFrameData[i].PrimaryCommandBuffer = IntPtr.Zero;
            }

            _perFrameData[i].PrimaryCommandPool.Dispose();

            if (_perFrameData[i].SwapchainAcquireSemaphore != VkSemaphore.Null)
            {
                vkDestroySemaphore(LogicalDevice, _perFrameData[i].SwapchainAcquireSemaphore, null);
                _perFrameData[i].SwapchainAcquireSemaphore = VkSemaphore.Null;
            }

            if (_perFrameData[i].SwapchainReleaseSemaphore != VkSemaphore.Null)
            {
                vkDestroySemaphore(LogicalDevice, _perFrameData[i].SwapchainReleaseSemaphore, null);
                _perFrameData[i].SwapchainReleaseSemaphore = VkSemaphore.Null;
            }
        }

        Synchronization.Dispose();

        // Pipeline.Dispose();

        CommandBufferManager.Dispose();

        CommandPool.Dispose();

        vkDestroyDevice(LogicalDevice, null);

        if (_surface != VkSurfaceKHR.Null)
        {
            vkDestroySurfaceKHR(VulkanInstance.Instance, _surface, null);
        }

        VulkanInstance.Dispose();
    }

    public override BackendPipelineBuilder CreatePipelineBuilder()
    {
        return new VulkanPipelineBuilder(this, SwapchainRenderTarget, ShaderManager, VulkanBufferManager);
    }

    public override BackendBufferManager BackendBufferManager => VulkanBufferManager;
    public override BackendImageManager BackendImageManager => VulkanImageManager;

    public override void RenderFrame(Action<BackendRenderFrameContext> action, [CallerMemberName] string? frameName = null)
    {
        VkResult result = AcquireNextImage(out CurrentSwapchainImageIndex);

        // Handle outdated error in acquire.
        if (result == VkResult.SuboptimalKHR || result == VkResult.ErrorOutOfDateKHR)
        {
            result = AcquireNextImage(out CurrentSwapchainImageIndex);
        }

        if (result != VkResult.Success)
        {
            vkDeviceWaitIdle(LogicalDevice);
            return;
        }

        // Begin command recording
        VkCommandBuffer cmd = _perFrameData[CurrentSwapchainImageIndex].PrimaryCommandBuffer;

        CommandBufferManager.BeginCommandBuffer(cmd);

        // Transition image layout to VK_IMAGE_LAYOUT_PRESENT_SRC_KHR
        TransitionImageLayout(cmd, ((VulkanImage)SwapchainRenderTarget.GetImage(CurrentSwapchainImageIndex)).Image, 
                              VK_IMAGE_LAYOUT_UNDEFINED, VK_IMAGE_LAYOUT_PRESENT_SRC_KHR);

        action(new VulkanRenderFrameContext(this, cmd, CurrentSwapchainImageIndex));

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
        vkQueueSubmit(GraphicsQueue, submitInfo, _perFrameData[CurrentSwapchainImageIndex].QueueSubmitFence);

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

    private void TransitionImageLayout(VkCommandBuffer commandBuffer, VkImage image, VkImageLayout oldLayout, VkImageLayout newLayout)
    {
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

        if (oldLayout == VK_IMAGE_LAYOUT_UNDEFINED && newLayout == VK_IMAGE_LAYOUT_PRESENT_SRC_KHR)
        {
            barrier.srcAccessMask = 0;
            barrier.dstAccessMask = VkAccessFlags.MemoryRead;

            sourceStage = VkPipelineStageFlags.TopOfPipe;
            destinationStage = VkPipelineStageFlags.BottomOfPipe;
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
    }

    public override BackendPassBuilder CreatePassBuilder()
    {
        return new VulkanPassBuilder();
    }

    private VkResult AcquireNextImage(out uint imageIndex)
    {
        VkSemaphore acquireSemaphore = Synchronization.AcquireSemaphore();

        VkResult result = vkAcquireNextImageKHR(LogicalDevice, SwapchainRenderTarget.VkSwapchain, ulong.MaxValue, acquireSemaphore, VkFence.Null, out imageIndex);

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
        => vkQueuePresentKHR(PresentQueue, _perFrameData[imageIndex].SwapchainReleaseSemaphore, SwapchainRenderTarget.VkSwapchain, imageIndex);

    public static implicit operator VkDevice(VulkanDevice device) => device.LogicalDevice;

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
        public VkFence QueueSubmitFence;
        public VulkanCommandPool PrimaryCommandPool;
        public VkCommandBuffer PrimaryCommandBuffer;
        public VkSemaphore SwapchainAcquireSemaphore;
        public VkSemaphore SwapchainReleaseSemaphore;
    }

    public void BeginRenderPass(VulkanPass pass, VkCommandBuffer commandBuffer, Vector2i size)
    {
        var targetImage = ((VulkanRenderTarget)pass.RenderTarget).GetImage(CurrentSwapchainImageIndex);

        VkRenderingAttachmentInfo colorAttachmentInfo = new VkRenderingAttachmentInfo
        {
            imageView = ((VulkanImage)targetImage).ImageView,
            imageLayout = ConvertImageLayout(pass.ColorAttachment.ImageLayout),
            loadOp = ConvertLoadOp(pass.ColorAttachment.LoadOp),
            storeOp = ConvertStoreOp(pass.ColorAttachment.StoreOp),
        };

        if (ClearColor.HasValue) 
            colorAttachmentInfo.clearValue = new VkClearValue { color = ClearColor.Value };

        VkRenderingInfo renderingInfo = new VkRenderingInfo
        {
            renderArea = new VkRect2D(VkOffset2D.Zero, size.ToVkExtent2D()),
            layerCount = 1,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachmentInfo
        };

        vkCmdBeginRendering(commandBuffer, &renderingInfo);
    }

    private VkImageLayout ConvertImageLayout(ImageLayout layout)
    {
        return layout switch
        {
            ImageLayout.Undefined => VkImageLayout.Undefined,
            ImageLayout.ColorAttachmentOptimal => VkImageLayout.ColorAttachmentOptimal,
            ImageLayout.PresentSrc => VkImageLayout.PresentSrcKHR,
            _ => throw new ArgumentOutOfRangeException(nameof(layout))
        };
    }

    private VkAttachmentLoadOp ConvertLoadOp(AttachmentLoadOp loadOp)
    {
        return loadOp switch
        {
            AttachmentLoadOp.Load => VkAttachmentLoadOp.Load,
            AttachmentLoadOp.Clear => VkAttachmentLoadOp.Clear,
            AttachmentLoadOp.DontCare => VkAttachmentLoadOp.DontCare,
            _ => throw new ArgumentOutOfRangeException(nameof(loadOp))
        };
    }

    private VkAttachmentStoreOp ConvertStoreOp(AttachmentStoreOp storeOp)
    {
        return storeOp switch
        {
            AttachmentStoreOp.Store => VkAttachmentStoreOp.Store,
            AttachmentStoreOp.DontCare => VkAttachmentStoreOp.DontCare,
            _ => throw new ArgumentOutOfRangeException(nameof(storeOp))
        };
    }

    public void BindPipeline(VkCommandBuffer commandBuffer, VkPipeline pipeline)
    {
        vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, pipeline);
    }

    public void EndRenderPass(VkCommandBuffer commandBuffer)
    {
        vkCmdEndRendering(commandBuffer);
    }

    public override BackendRenderTarget GetSwapchainRenderTarget()
    {
        return SwapchainRenderTarget;
    }
}