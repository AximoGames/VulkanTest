using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using System.Collections.Generic;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;

namespace Vortice
{

    public unsafe sealed class GraphicsDevice : IDisposable
    {
        private static readonly VkString s_EngineName = new VkString("Vortice");
        private static readonly string[] s_RequestedValidationLayers = new[] { "VK_LAYER_KHRONOS_validation" };

        public readonly VkInstance VkInstance;

        private readonly VkDebugUtilsMessengerEXT _debugMessenger = VkDebugUtilsMessengerEXT.Null;
        internal readonly VkSurfaceKHR _surface;
        public readonly VkPhysicalDevice PhysicalDevice;
        public readonly VkDevice VkDevice;
        public readonly VkQueue GraphicsQueue;
        public readonly VkQueue PresentQueue;
        public readonly Swapchain Swapchain;
        private PerFrame[] _perFrame; // TODO: Pin during init?
        public VkRenderPass RenderPass;

        private readonly List<VkSemaphore> _recycledSemaphores = new List<VkSemaphore>();

        public GraphicsDevice(string applicationName, bool enableValidation, GameWindow window)
        {
            CreateInstance(applicationName, enableValidation, out VkInstance, out _debugMessenger);

            _surface = CreateSurface(window);

            CreateDevice(out PhysicalDevice, out VkDevice, out GraphicsQueue, out PresentQueue);

            // Create swap chain
            Swapchain = new Swapchain(this, window);

            GetImages();
            CreateImageView();
            CreateRenderPass(Swapchain.SurfaceFormat.format);

            CreateCommandPool();
            CreateFrameBuffers();

            for (var i = 0; i < _perFrame.Length; i++)
            {
                VkFenceCreateInfo fenceCreateInfo = new VkFenceCreateInfo(VkFenceCreateFlags.Signaled);
                vkCreateFence(VkDevice, &fenceCreateInfo, null, out _perFrame[i].QueueSubmitFence).CheckResult();

                vkAllocateCommandBuffer(VkDevice, _perFrame[i].PrimaryCommandPool, out _perFrame[i].PrimaryCommandBuffer).CheckResult();
            }
        }

        private void CreateInstance(string applicationName, bool enableValidation, out VkInstance instance, out VkDebugUtilsMessengerEXT _debugMessenger)
        {
            using VkString name = applicationName;
            var appInfo = new VkApplicationInfo
            {
                sType = VkStructureType.ApplicationInfo,
                pApplicationName = name,
                applicationVersion = new VkVersion(1, 0, 0),
                pEngineName = s_EngineName,
                engineVersion = new VkVersion(1, 0, 0),
                apiVersion = vkEnumerateInstanceVersion()
            };

            List<string> instanceExtensions = new List<string>();

            instanceExtensions.AddRange(GLFW.GetRequiredInstanceExtensions());

            // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            // {
            //     instanceExtensions.Add(KHRWin32SurfaceExtensionName);
            // }

            // if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            // {
            //     instanceExtensions.Add(KHRXlibSurfaceExtensionName);
            // }

            List<string> instanceLayers = new List<string>();
            if (enableValidation)
            {
                FindValidationLayers(instanceLayers);
            }

            if (instanceLayers.Count > 0)
            {
                instanceExtensions.Add(EXTDebugUtilsExtensionName);
            }

            using var vkInstanceExtensions = new VkStringArray(instanceExtensions);

            var instanceCreateInfo = new VkInstanceCreateInfo
            {
                sType = VkStructureType.InstanceCreateInfo,
                pApplicationInfo = &appInfo,
                enabledExtensionCount = vkInstanceExtensions.Length,
                ppEnabledExtensionNames = vkInstanceExtensions
            };

            using var vkLayerNames = new VkStringArray(instanceLayers);
            if (instanceLayers.Count > 0)
            {
                instanceCreateInfo.enabledLayerCount = vkLayerNames.Length;
                instanceCreateInfo.ppEnabledLayerNames = vkLayerNames;
            }

            var debugUtilsCreateInfo = new VkDebugUtilsMessengerCreateInfoEXT
            {
                sType = VkStructureType.DebugUtilsMessengerCreateInfoEXT
            };

            if (instanceLayers.Count > 0)
            {
                debugUtilsCreateInfo.messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Error | VkDebugUtilsMessageSeverityFlagsEXT.Warning;
                debugUtilsCreateInfo.messageType = VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance;
                debugUtilsCreateInfo.pfnUserCallback = &DebugMessengerCallback;

                instanceCreateInfo.pNext = &debugUtilsCreateInfo;
            }

            VkResult result = vkCreateInstance(&instanceCreateInfo, null, out instance);
            if (result != VkResult.Success)
            {
                throw new InvalidOperationException($"Failed to create vulkan instance: {result}");
            }

            vkLoadInstance(VkInstance);

            if (instanceLayers.Count > 0)
            {
                vkCreateDebugUtilsMessengerEXT(VkInstance, &debugUtilsCreateInfo, null, out _debugMessenger).CheckResult();
            }
            else
            {
                _debugMessenger = VkDebugUtilsMessengerEXT.Null;
            }

            Log.Info($"Created VkInstance with version: {appInfo.apiVersion.Major}.{appInfo.apiVersion.Minor}.{appInfo.apiVersion.Patch}");
            if (instanceLayers.Count > 0)
            {
                foreach (var layer in instanceLayers)
                {
                    Log.Info($"Instance layer '{layer}'");
                }
            }

            foreach (string extension in instanceExtensions)
            {
                Log.Info($"Instance extension '{extension}'");
            }
        }

        public void CreateDevice(out VkPhysicalDevice physicalDevice, out VkDevice device, out VkQueue graphicsQueue, out VkQueue presentQueue)
        {             // Find physical device, setup queue's and create device.
            var physicalDevices = vkEnumeratePhysicalDevices(VkInstance);
            foreach (var pDevice in physicalDevices)
            {
                //vkGetPhysicalDeviceProperties(pDevice, out var properties);
                //var deviceName = properties.GetDeviceName();
            }

            physicalDevice = physicalDevices[0];
            vkGetPhysicalDeviceProperties(physicalDevice, out VkPhysicalDeviceProperties properties);

            queueFamilies = FindQueueFamilies(physicalDevice, _surface);

            var availableDeviceExtensions = vkEnumerateDeviceExtensionProperties(physicalDevice);

            // var supportPresent = vkGetPhysicalDeviceWin32PresentationSupportKHR(PhysicalDevice, queueFamilies.graphicsFamily);

            float priority = 1.0f;
            VkDeviceQueueCreateInfo queueCreateInfo = new VkDeviceQueueCreateInfo
            {
                sType = VkStructureType.DeviceQueueCreateInfo,
                queueFamilyIndex = queueFamilies.graphicsFamily,
                queueCount = 1,
                pQueuePriorities = &priority
            };

            List<string> enabledExtensions = new List<string>
            {
                KHRSwapchainExtensionName
            };

            VkPhysicalDeviceVulkan11Features features_1_1 = new VkPhysicalDeviceVulkan11Features
            {
                sType = VkStructureType.PhysicalDeviceVulkan11Features
            };

            VkPhysicalDeviceVulkan12Features features_1_2 = new VkPhysicalDeviceVulkan12Features
            {
                sType = VkStructureType.PhysicalDeviceVulkan12Features
            };

            VkPhysicalDeviceFeatures2 deviceFeatures2 = new VkPhysicalDeviceFeatures2
            {
                sType = VkStructureType.PhysicalDeviceFeatures2
            };

            deviceFeatures2.pNext = &features_1_1;
            features_1_1.pNext = &features_1_2;

            void** features_chain = &features_1_2.pNext;

            VkPhysicalDevice8BitStorageFeatures storage_8bit_features = default;
            if (properties.apiVersion <= VkVersion.Version_1_2)
            {
                if (CheckDeviceExtensionSupport(KHR8bitStorageExtensionName, availableDeviceExtensions))
                {
                    enabledExtensions.Add(KHR8bitStorageExtensionName);
                    storage_8bit_features.sType = VkStructureType.PhysicalDevice8bitStorageFeatures;
                    *features_chain = &storage_8bit_features;
                    features_chain = &storage_8bit_features.pNext;
                }
            }

            if (CheckDeviceExtensionSupport(KHRSpirv14ExtensionName, availableDeviceExtensions))
            {
                // Required for VK_KHR_ray_tracing_pipeline
                enabledExtensions.Add(KHRSpirv14ExtensionName);

                // Required by VK_KHR_spirv_1_4
                enabledExtensions.Add(KHRShaderFloatControlsExtensionName);
            }

            if (CheckDeviceExtensionSupport(KHRBufferDeviceAddressExtensionName, availableDeviceExtensions))
            {
                // Required by VK_KHR_acceleration_structure
                enabledExtensions.Add(KHRBufferDeviceAddressExtensionName);
            }

            if (CheckDeviceExtensionSupport(EXTDescriptorIndexingExtensionName, availableDeviceExtensions))
            {
                // Required by VK_KHR_acceleration_structure
                enabledExtensions.Add(EXTDescriptorIndexingExtensionName);
            }

            VkPhysicalDeviceAccelerationStructureFeaturesKHR acceleration_structure_features = default;
            if (CheckDeviceExtensionSupport(KHRAccelerationStructureExtensionName, availableDeviceExtensions))
            {
                // Required by VK_KHR_acceleration_structure
                enabledExtensions.Add(KHRDeferredHostOperationsExtensionName);

                enabledExtensions.Add(KHRAccelerationStructureExtensionName);
                acceleration_structure_features.sType = VkStructureType.PhysicalDeviceAccelerationStructureFeaturesKHR;
                *features_chain = &acceleration_structure_features;
                features_chain = &acceleration_structure_features.pNext;
            }

            vkGetPhysicalDeviceFeatures2(physicalDevice, out deviceFeatures2);

            using var deviceExtensionNames = new VkStringArray(enabledExtensions);

            var deviceCreateInfo = new VkDeviceCreateInfo
            {
                sType = VkStructureType.DeviceCreateInfo,
                pNext = &deviceFeatures2,
                queueCreateInfoCount = 1,
                pQueueCreateInfos = &queueCreateInfo,
                enabledExtensionCount = deviceExtensionNames.Length,
                ppEnabledExtensionNames = deviceExtensionNames,
                pEnabledFeatures = null,
            };

            var result = vkCreateDevice(physicalDevice, &deviceCreateInfo, null, out device);
            if (result != VkResult.Success)
                throw new Exception($"Failed to create Vulkan Logical Device, {result}");

            vkGetDeviceQueue(device, queueFamilies.graphicsFamily, 0, out graphicsQueue);
            vkGetDeviceQueue(device, queueFamilies.presentFamily, 0, out presentQueue);
        }

        private void GetImages()
        {
            ReadOnlySpan<VkImage> swapChainImages = vkGetSwapchainImagesKHR(VkDevice, Swapchain.Handle);
            _perFrame = new PerFrame[swapChainImages.Length];
            for (var i = 0; i < swapChainImages.Length; i++)
            {
                _perFrame[i].Image = swapChainImages[i];
            }
        }

        private void CreateImageView()
        {
            for (int i = 0; i < _perFrame.Length; i++)
            {
                var viewCreateInfo = new VkImageViewCreateInfo(
                    _perFrame[i].Image,
                    VkImageViewType.Image2D,
                    Swapchain.SurfaceFormat.format,
                    VkComponentMapping.Rgba,
                    new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1)
                    );

                vkCreateImageView(VkDevice, &viewCreateInfo, null, out _perFrame[i].ImageView).CheckResult();
            }
        }

        private void CreateRenderPass(VkFormat colorFormat)
        {
            VkAttachmentDescription attachment = new VkAttachmentDescription(
                colorFormat,
                VkSampleCountFlags.Count1,
                VkAttachmentLoadOp.Clear, VkAttachmentStoreOp.Store,
                VkAttachmentLoadOp.DontCare, VkAttachmentStoreOp.DontCare,
                VkImageLayout.Undefined, VkImageLayout.PresentSrcKHR
            );

            VkAttachmentReference colorAttachmentRef = new VkAttachmentReference(0, VkImageLayout.ColorAttachmentOptimal);

            VkSubpassDescription subpass = new VkSubpassDescription
            {
                pipelineBindPoint = VkPipelineBindPoint.Graphics,
                colorAttachmentCount = 1,
                pColorAttachments = &colorAttachmentRef
            };

            VkSubpassDependency[] dependencies = new VkSubpassDependency[2];

            dependencies[0] = new VkSubpassDependency
            {
                srcSubpass = SubpassExternal,
                dstSubpass = 0,
                srcStageMask = VkPipelineStageFlags.BottomOfPipe,
                dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                srcAccessMask = VkAccessFlags.MemoryRead,
                dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite,
                dependencyFlags = VkDependencyFlags.ByRegion
            };

            dependencies[1] = new VkSubpassDependency
            {
                srcSubpass = 0,
                dstSubpass = SubpassExternal,
                srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
                dstStageMask = VkPipelineStageFlags.BottomOfPipe,
                srcAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite,
                dstAccessMask = VkAccessFlags.MemoryRead,
                dependencyFlags = VkDependencyFlags.ByRegion
            };

            fixed (VkSubpassDependency* dependenciesPtr = &dependencies[0])
            {
                VkRenderPassCreateInfo createInfo = new VkRenderPassCreateInfo
                {
                    sType = VkStructureType.RenderPassCreateInfo,
                    attachmentCount = 1,
                    pAttachments = &attachment,
                    subpassCount = 1,
                    pSubpasses = &subpass,
                    dependencyCount = 2,
                    pDependencies = dependenciesPtr
                };

                vkCreateRenderPass(VkDevice, &createInfo, null, out RenderPass).CheckResult();
            }
        }

        private void CreateCommandPool()
        {
            for (var i = 0; i < _perFrame.Length; i++)
            {
                VkCommandPoolCreateInfo poolCreateInfo = new VkCommandPoolCreateInfo
                {
                    sType = VkStructureType.CommandPoolCreateInfo,
                    flags = VkCommandPoolCreateFlags.Transient,
                    queueFamilyIndex = queueFamilies.graphicsFamily,
                };
                vkCreateCommandPool(VkDevice, &poolCreateInfo, null, out _perFrame[i].PrimaryCommandPool).CheckResult();
            }
        }

        private static bool CheckDeviceExtensionSupport(string extensionName, ReadOnlySpan<VkExtensionProperties> availableDeviceExtensions)
        {
            foreach (VkExtensionProperties property in availableDeviceExtensions)
            {
                if (string.Equals(property.GetExtensionName(), extensionName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private void CreateFrameBuffers()
        {
            for (var i = 0; i < _perFrame.Length; i++)
            {
                vkCreateFramebuffer(VkDevice, RenderPass, new[] { _perFrame[i].ImageView }, Swapchain.Extent, 1u, out _perFrame[i].Framebuffer);
            }
        }

        public void Dispose()
        {
            // Don't release anything until the GPU is completely idle.
            vkDeviceWaitIdle(VkDevice);

            Swapchain.Dispose();

            for (var i = 0; i < _perFrame.Length; i++)
            {
                vkDestroyFence(VkDevice, _perFrame[i].QueueSubmitFence, null);

                if (_perFrame[i].PrimaryCommandBuffer != IntPtr.Zero)
                {
                    vkFreeCommandBuffers(VkDevice, _perFrame[i].PrimaryCommandPool, _perFrame[i].PrimaryCommandBuffer);

                    _perFrame[i].PrimaryCommandBuffer = IntPtr.Zero;
                }

                vkDestroyCommandPool(VkDevice, _perFrame[i].PrimaryCommandPool, null);

                if (_perFrame[i].SwapchainAcquireSemaphore != VkSemaphore.Null)
                {
                    vkDestroySemaphore(VkDevice, _perFrame[i].SwapchainAcquireSemaphore, null);
                    _perFrame[i].SwapchainAcquireSemaphore = VkSemaphore.Null;
                }

                if (_perFrame[i].SwapchainReleaseSemaphore != VkSemaphore.Null)
                {
                    vkDestroySemaphore(VkDevice, _perFrame[i].SwapchainReleaseSemaphore, null);
                    _perFrame[i].SwapchainReleaseSemaphore = VkSemaphore.Null;
                }
            }

            foreach (VkSemaphore semaphore in _recycledSemaphores)
            {
                vkDestroySemaphore(VkDevice, semaphore, null);
            }
            _recycledSemaphores.Clear();

            if (VkDevice != VkDevice.Null)
            {
                vkDestroyDevice(VkDevice, null);
            }

            if (_surface != VkSurfaceKHR.Null)
            {
                vkDestroySurfaceKHR(VkInstance, _surface, null);
            }

            if (_debugMessenger != VkDebugUtilsMessengerEXT.Null)
            {
                vkDestroyDebugUtilsMessengerEXT(VkInstance, _debugMessenger, null);
            }

            if (VkInstance != VkInstance.Null)
            {
                vkDestroyInstance(VkInstance, null);
            }
        }

        public void RenderFrame(Action<VkCommandBuffer, VkFramebuffer, VkExtent2D> draw, [CallerMemberName] string? frameName = null)
        {
            VkResult result = AcquireNextImage(out uint swapchainIndex);

            // Handle outdated error in acquire.
            if (result == VkResult.SuboptimalKHR || result == VkResult.ErrorOutOfDateKHR)
            {
                //Resize(context.swapchain_dimensions.width, context.swapchain_dimensions.height);
                result = AcquireNextImage(out swapchainIndex);
            }

            if (result != VkResult.Success)
            {
                vkDeviceWaitIdle(VkDevice);
                return;
            }

            // Begin command recording
            VkCommandBuffer cmd = _perFrame[swapchainIndex].PrimaryCommandBuffer;

            VkCommandBufferBeginInfo beginInfo = new VkCommandBufferBeginInfo
            {
                sType = VkStructureType.CommandBufferBeginInfo,
                flags = VkCommandBufferUsageFlags.OneTimeSubmit
            };
            vkBeginCommandBuffer(cmd, &beginInfo).CheckResult();

            draw(cmd, _perFrame[swapchainIndex].Framebuffer, Swapchain.Extent);

            // Complete the command buffer.
            vkEndCommandBuffer(cmd).CheckResult();

            if (_perFrame[swapchainIndex].SwapchainReleaseSemaphore == VkSemaphore.Null)
            {
                vkCreateSemaphore(VkDevice, out _perFrame[swapchainIndex].SwapchainReleaseSemaphore).CheckResult();
            }

            VkPipelineStageFlags wait_stage = VkPipelineStageFlags.ColorAttachmentOutput;
            VkSemaphore waitSemaphore = _perFrame[swapchainIndex].SwapchainAcquireSemaphore;
            VkSemaphore signalSemaphore = _perFrame[swapchainIndex].SwapchainReleaseSemaphore;

            VkSubmitInfo submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo,
                commandBufferCount = 1u,
                pCommandBuffers = &cmd,
                waitSemaphoreCount = 1u,
                pWaitSemaphores = &waitSemaphore,
                pWaitDstStageMask = &wait_stage,
                signalSemaphoreCount = 1u,
                pSignalSemaphores = &signalSemaphore
            };

            // Submit command buffer to graphics queue
            vkQueueSubmit(GraphicsQueue, submitInfo, _perFrame[swapchainIndex].QueueSubmitFence);

            result = PresentImage(swapchainIndex);

            // Handle Outdated error in present.
            if (result == VkResult.SuboptimalKHR || result == VkResult.ErrorOutOfDateKHR)
            {
                //Resize(context.swapchain_dimensions.width, context.swapchain_dimensions.height);
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
                vkCreateSemaphore(VkDevice, out acquireSemaphore).CheckResult();
            }
            else
            {
                acquireSemaphore = _recycledSemaphores[_recycledSemaphores.Count - 1];
                _recycledSemaphores.RemoveAt(_recycledSemaphores.Count - 1);
            }

            VkResult result = vkAcquireNextImageKHR(VkDevice, Swapchain.Handle, ulong.MaxValue, acquireSemaphore, VkFence.Null, out imageIndex);

            if (result != VkResult.Success)
            {
                _recycledSemaphores.Add(acquireSemaphore);
                return result;
            }

            if (_perFrame[imageIndex].QueueSubmitFence != VkFence.Null)
            {
                vkWaitForFences(VkDevice, _perFrame[imageIndex].QueueSubmitFence, true, ulong.MaxValue);
                vkResetFences(VkDevice, _perFrame[imageIndex].QueueSubmitFence);
            }

            if (_perFrame[imageIndex].PrimaryCommandPool != VkCommandPool.Null)
            {
                vkResetCommandPool(VkDevice, _perFrame[imageIndex].PrimaryCommandPool, VkCommandPoolResetFlags.None);
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
            return vkQueuePresentKHR(PresentQueue, _perFrame[imageIndex].SwapchainReleaseSemaphore, Swapchain.Handle, imageIndex);
        }

        public static implicit operator VkDevice(GraphicsDevice device) => device.VkDevice;

        #region Private Methods
        private VkSurfaceKHR CreateSurface(GameWindow window)
        {
            GLFW.CreateWindowSurface(new VkHandle(VkInstance.Handle), (Window*)window.Context.WindowPtr, null, out var handle);
            return new VkSurfaceKHR((ulong)handle.Handle);
        }

#if NET5_0
        [UnmanagedCallersOnly]
#endif
        private static uint DebugMessengerCallback(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity,
            VkDebugUtilsMessageTypeFlagsEXT messageTypes,
            VkDebugUtilsMessengerCallbackDataEXT* pCallbackData,
            void* userData)
        {
            string? message = Interop.GetString(pCallbackData->pMessage);
            if (messageTypes == VkDebugUtilsMessageTypeFlagsEXT.Validation)
            {
                if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Error)
                {
                    Log.Error($"[Vulkan]: Validation: {messageSeverity} - {message}");
                }
                else if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Warning)
                {
                    Log.Warn($"[Vulkan]: Validation: {messageSeverity} - {message}");
                }

                Debug.WriteLine($"[Vulkan]: Validation: {messageSeverity} - {message}");
            }
            else
            {
                if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Error)
                {
                    Log.Error($"[Vulkan]: {messageSeverity} - {message}");
                }
                else if (messageSeverity == VkDebugUtilsMessageSeverityFlagsEXT.Warning)
                {
                    Log.Warn($"[Vulkan]: {messageSeverity} - {message}");
                }

                Debug.WriteLine($"[Vulkan]: {messageSeverity} - {message}");
            }

            return VK_FALSE;
        }

        private static void FindValidationLayers(List<string> appendTo)
        {
            ReadOnlySpan<VkLayerProperties> availableLayers = vkEnumerateInstanceLayerProperties();

            for (int i = 0; i < s_RequestedValidationLayers.Length; i++)
            {
                bool hasLayer = false;
                for (int j = 0; j < availableLayers.Length; j++)
                {
                    if (s_RequestedValidationLayers[i] == availableLayers[j].GetLayerName())
                    {
                        hasLayer = true;
                        break;
                    }
                }

                if (hasLayer)
                {
                    appendTo.Add(s_RequestedValidationLayers[i]);
                }
                else
                {
                    // TODO: Warn
                }
            }
        }

        private (uint graphicsFamily, uint presentFamily) queueFamilies;

        static (uint graphicsFamily, uint presentFamily) FindQueueFamilies(
            VkPhysicalDevice device, VkSurfaceKHR surface)
        {
            ReadOnlySpan<VkQueueFamilyProperties> queueFamilies = vkGetPhysicalDeviceQueueFamilyProperties(device);

            uint graphicsFamily = QueueFamilyIgnored;
            uint presentFamily = QueueFamilyIgnored;
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

                if (graphicsFamily != QueueFamilyIgnored
                    && presentFamily != QueueFamilyIgnored)
                {
                    break;
                }

                i++;
            }

            return (graphicsFamily, presentFamily);
        }
        #endregion

        private struct PerFrame
        {
            public VkImage Image;
            public VkImageView ImageView;
            public VkFramebuffer Framebuffer;
            public VkFence QueueSubmitFence;
            public VkCommandPool PrimaryCommandPool;
            public VkCommandBuffer PrimaryCommandBuffer;
            public VkSemaphore SwapchainAcquireSemaphore;
            public VkSemaphore SwapchainReleaseSemaphore;
        }
    }
}