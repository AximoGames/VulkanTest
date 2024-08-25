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
    private static readonly string[] s_RequestedValidationLayers = new[] { "VK_LAYER_KHRONOS_validation" };

    public readonly VkInstance VkInstance;

    private readonly VkDebugUtilsMessengerEXT _debugMessenger = VkDebugUtilsMessengerEXT.Null;
    internal readonly VkSurfaceKHR _surface;
    public readonly VkPhysicalDevice PhysicalDevice;
    public readonly VkDevice VkDevice;
    public readonly VkQueue GraphicsQueue;
    public readonly VkQueue PresentQueue;
    public readonly Swapchain Swapchain;
    public readonly VkPipeline Pipeline;
    private PerFrame[] _perFrame; // TODO: Pin during init?
    public VkBuffer VertexBuffer;
    public VkBuffer IndexBuffer;
    public VkDeviceMemory IndexBufferMemory;
    public VkDeviceMemory VertexBufferMemory;
    public VkCommandPool CommandPool;

    private readonly List<VkSemaphore> _recycledSemaphores = new List<VkSemaphore>();

    public uint CurrentSwapchainImageIndex;

    public GraphicsDevice(string applicationName, bool enableValidation, GameWindow window)
    {
        CreateInstance(applicationName, enableValidation, out VkInstance, out _debugMessenger);

        _surface = CreateSurface(window);

        CreateDevice(out PhysicalDevice, out VkDevice, out GraphicsQueue, out PresentQueue);

        // Create swap chain
        Swapchain = new Swapchain(this, window);

        GetImages();
        CreateImageView();
        CreateGraphicsPipeline(out Pipeline);

        CreateCommandPool();

        CreateVertexBuffer();
        CreateIndexBuffer();

        for (var i = 0; i < _perFrame.Length; i++)
        {
            VkFenceCreateInfo fenceCreateInfo = new VkFenceCreateInfo(VkFenceCreateFlags.Signaled);
            vkCreateFence(VkDevice, &fenceCreateInfo, null, out _perFrame[i].QueueSubmitFence).CheckResult();

            vkAllocateCommandBuffer(VkDevice, _perFrame[i].PrimaryCommandPool, out _perFrame[i].PrimaryCommandBuffer).CheckResult();
        }
    }

    private void CreateInstance(string applicationName, bool enableValidation, out VkInstance instance, out VkDebugUtilsMessengerEXT _debugMessenger)
    {
        var appInfo = new VkApplicationInfo
        {
            pApplicationName = applicationName.ToVkUtf8ReadOnlyString(),
            applicationVersion = new VkVersion(1, 0, 0),
            pEngineName = s_EngineName.ToVkUtf8ReadOnlyString(),
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
            instanceExtensions.Add(VK_EXT_DEBUG_UTILS_EXTENSION_NAME.GetStringFromUtf8Buffer());
        }

        //instanceExtensions.Add("VK_KHR_buffer_device_address");

        using var vkInstanceExtensions = new VkStringArray(instanceExtensions);

        var instanceCreateInfo = new VkInstanceCreateInfo
        {
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
        };

        if (instanceLayers.Count > 0)
        {
            debugUtilsCreateInfo.messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Error | VkDebugUtilsMessageSeverityFlagsEXT.Warning;
            debugUtilsCreateInfo.messageType = VkDebugUtilsMessageTypeFlagsEXT.Validation | VkDebugUtilsMessageTypeFlagsEXT.Performance | VkDebugUtilsMessageTypeFlagsEXT.General;
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

        GetAvailabeExtensions();
    }

    public void CreateDevice(out VkPhysicalDevice physicalDevice, out VkDevice device, out VkQueue graphicsQueue, out VkQueue presentQueue)
    {
        // Find physical device, setup queue's and create device.
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
        Log.Verbose($"Found {availableDeviceExtensions.Length} device extensions:");
        foreach (var extension in availableDeviceExtensions)
        {
            var name = extension.GetExtensionName();
            Log.Verbose(name);
        }

        // var supportPresent = vkGetPhysicalDeviceWin32PresentationSupportKHR(PhysicalDevice, queueFamilies.graphicsFamily);

        float priority = 1.0f;
        VkDeviceQueueCreateInfo queueCreateInfo = new VkDeviceQueueCreateInfo
        {
            queueFamilyIndex = queueFamilies.graphicsFamily,
            queueCount = 1,
            pQueuePriorities = &priority
        };

        List<string> enabledExtensions = new List<string>
        {
            VK_KHR_SWAPCHAIN_EXTENSION_NAME.GetStringFromUtf8Buffer()
        };

        VkPhysicalDeviceVulkan11Features features_1_1 = new VkPhysicalDeviceVulkan11Features
        {
        };

        VkPhysicalDeviceVulkan12Features features_1_2 = new VkPhysicalDeviceVulkan12Features
        {
        };

        VkPhysicalDeviceFeatures2 deviceFeatures2 = new VkPhysicalDeviceFeatures2
        {
        };

        VkPhysicalDeviceDynamicRenderingFeatures dynamicRenderingFeatures = new VkPhysicalDeviceDynamicRenderingFeatures
        {
            dynamicRendering = VkBool32.True
        };

        deviceFeatures2.pNext = &features_1_1;
        features_1_1.pNext = &features_1_2;
        features_1_2.pNext = &dynamicRenderingFeatures;

        void** features_chain = &features_1_2.pNext;

        VkPhysicalDevice8BitStorageFeatures storage_8bit_features = default;
        if (properties.apiVersion <= VkVersion.Version_1_2)
        {
            if (CheckDeviceExtensionSupport(VK_KHR_8BIT_STORAGE_EXTENSION_NAME.GetStringFromUtf8Buffer(), availableDeviceExtensions))
            {
                enabledExtensions.Add(VK_KHR_8BIT_STORAGE_EXTENSION_NAME.GetStringFromUtf8Buffer());
                *features_chain = &storage_8bit_features;
                features_chain = &storage_8bit_features.pNext;
            }
        }

        if (CheckDeviceExtensionSupport(VK_KHR_SPIRV_1_4_EXTENSION_NAME.GetStringFromUtf8Buffer(), availableDeviceExtensions))
        {
            // Required for VK_KHR_ray_tracing_pipeline
            enabledExtensions.Add(VK_KHR_SPIRV_1_4_EXTENSION_NAME.GetStringFromUtf8Buffer());

            // Required by VK_KHR_spirv_1_4
            enabledExtensions.Add(VK_KHR_SHADER_FLOAT_CONTROLS_EXTENSION_NAME.GetStringFromUtf8Buffer());
        }

        if (CheckDeviceExtensionSupport(VK_KHR_BUFFER_DEVICE_ADDRESS_EXTENSION_NAME.GetStringFromUtf8Buffer(), availableDeviceExtensions))
        {
            // Required by VK_KHR_acceleration_structure
            enabledExtensions.Add(VK_KHR_BUFFER_DEVICE_ADDRESS_EXTENSION_NAME.GetStringFromUtf8Buffer());
        }

        if (CheckDeviceExtensionSupport(VK_EXT_DESCRIPTOR_INDEXING_EXTENSION_NAME.GetStringFromUtf8Buffer(), availableDeviceExtensions))
        {
            // Required by VK_KHR_acceleration_structure
            enabledExtensions.Add(VK_EXT_DESCRIPTOR_INDEXING_EXTENSION_NAME.GetStringFromUtf8Buffer());
        }

        VkPhysicalDeviceAccelerationStructureFeaturesKHR acceleration_structure_features = default;
        if (CheckDeviceExtensionSupport(VK_KHR_ACCELERATION_STRUCTURE_EXTENSION_NAME.GetStringFromUtf8Buffer(), availableDeviceExtensions))
        {
            // Required by VK_KHR_acceleration_structure
            enabledExtensions.Add(VK_KHR_DEFERRED_HOST_OPERATIONS_EXTENSION_NAME.GetStringFromUtf8Buffer());

            enabledExtensions.Add(VK_KHR_ACCELERATION_STRUCTURE_EXTENSION_NAME.GetStringFromUtf8Buffer());
            *features_chain = &acceleration_structure_features;
            features_chain = &acceleration_structure_features.pNext;
        }

        vkGetPhysicalDeviceFeatures2(physicalDevice, &deviceFeatures2);

        using var deviceExtensionNames = new VkStringArray(enabledExtensions);

        var deviceCreateInfo = new VkDeviceCreateInfo
        {
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

    private void GetAvailabeExtensions()
    {
        uint count;
        var result = vkEnumerateInstanceExtensionProperties(&count, null);
        if (result != VkResult.Success)
            throw new Exception($"Failed to enumerate instance extensions, {result}");

        var properties = new VkExtensionProperties[count];
        result = vkEnumerateInstanceExtensionProperties(properties);
        if (result != VkResult.Success)
            throw new Exception($"Failed to enumerate instance extensions, {result}");

        Log.Verbose($"Found {count} instance extensions:");
        foreach (var prop in properties)
        {
            var name = prop.GetExtensionName();
            Log.Verbose(name);
        }
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

    private VkShaderModule CreateShaderModuleFromCode(string shaderCode, ShaderKind shaderKind)
    {
        using Compiler compiler = new Compiler();
        using (var compilationResult = compiler.Compile(shaderCode, "main", shaderKind))
        {
            vkCreateShaderModule(VkDevice, compilationResult.GetBytecode(), null, out VkShaderModule module).CheckResult();
            return module;
        }
    }

    public struct Vertex
    {
        public Vector2 pos;
        public Vector3 color;

        public static VkVertexInputBindingDescription getBindingDescription()
        {
            VkVertexInputBindingDescription bindingDescription;
            bindingDescription.binding = 0;
            bindingDescription.stride = (uint)Marshal.SizeOf<Vertex>();
            bindingDescription.inputRate = VkVertexInputRate.Vertex;
            return bindingDescription;
        }

        public static VkVertexInputAttributeDescription[] getAttributeDescriptions()
        {
            var attributeDescriptions = new VkVertexInputAttributeDescription[2];

            attributeDescriptions[0].binding = 0;
            attributeDescriptions[0].location = 0;
            attributeDescriptions[0].format = VkFormat.R32G32Sfloat;
            attributeDescriptions[0].offset = 0;

            attributeDescriptions[1].binding = 0;
            attributeDescriptions[1].location = 1;
            attributeDescriptions[1].format = VkFormat.R32G32B32Sfloat;
            attributeDescriptions[1].offset = (uint)Marshal.SizeOf<Vector2>();

            return attributeDescriptions;
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

    private void CreateGraphicsPipeline(out VkPipeline pipeline)
    {
        //auto vertShaderCode = readFile("shaders/vert.spv");
        //auto fragShaderCode = readFile("shaders/frag.spv");

        // language=glsl
        string vertexShaderCode =
            """
            #version 450

            layout(location = 0) in vec2 inPosition;
            layout(location = 1) in vec3 inColor;

            layout(location = 0) out vec3 fragColor;

            void main() {
                gl_Position = vec4(inPosition, 0.0, 1.0);
                fragColor = inColor;
            }
            """;

        // language=glsl
        string fragShaderCode =
            """
            #version 450

            layout(location = 0) in vec3 fragColor;

            layout(location = 0) out vec4 outColor;

            void main() {
                outColor = vec4(fragColor, 1.0);
            }
            """;

        VkShaderModule vertShaderModule = CreateShaderModuleFromCode(vertexShaderCode, ShaderKind.VertexShader);
        VkShaderModule fragShaderModule = CreateShaderModuleFromCode(fragShaderCode, ShaderKind.FragmentShader);

        var name = "main".ToVkUtf8ReadOnlyString();

        var vertShaderStageInfo = new VkPipelineShaderStageCreateInfo();
        vertShaderStageInfo.stage = VkShaderStageFlags.Vertex;
        vertShaderStageInfo.module = vertShaderModule;
        vertShaderStageInfo.pName = name;

        var fragShaderStageInfo = new VkPipelineShaderStageCreateInfo();
        fragShaderStageInfo.stage = VkShaderStageFlags.Fragment;
        fragShaderStageInfo.module = fragShaderModule;
        fragShaderStageInfo.pName = name;

        var bindingDescription = Vertex.getBindingDescription();
        var attributeDescriptions = Vertex.getAttributeDescriptions();

        fixed (VkVertexInputAttributeDescription* attributeDescriptionsPtr = &attributeDescriptions[0])
        {
            var vertexInputInfo = new VkPipelineVertexInputStateCreateInfo();

            vertexInputInfo.vertexBindingDescriptionCount = 1;
            vertexInputInfo.vertexAttributeDescriptionCount = (uint)attributeDescriptions.Length;

            vertexInputInfo.pVertexBindingDescriptions = &bindingDescription;
            vertexInputInfo.pVertexAttributeDescriptions = attributeDescriptionsPtr;

            var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo();
            inputAssembly.topology = VkPrimitiveTopology.TriangleList;
            inputAssembly.primitiveRestartEnable = VkBool32.False;

            var viewport = new VkViewport();
            viewport.x = 0.0f;
            viewport.y = 0.0f;
            viewport.width = Swapchain.Extent.width;
            viewport.height = Swapchain.Extent.height;
            viewport.minDepth = 0.0f;
            viewport.maxDepth = 1.0f;

            var scissor = new VkRect2D();
            scissor.offset = new VkOffset2D(0, 0);
            scissor.extent = Swapchain.Extent;

            var viewportState = new VkPipelineViewportStateCreateInfo();
            viewportState.viewportCount = 1;
            viewportState.pViewports = &viewport;
            viewportState.scissorCount = 1;
            viewportState.pScissors = &scissor;

            var rasterizer = new VkPipelineRasterizationStateCreateInfo();
            rasterizer.depthClampEnable = VkBool32.False;
            rasterizer.rasterizerDiscardEnable = VkBool32.False;
            rasterizer.polygonMode = VkPolygonMode.Fill;
            rasterizer.lineWidth = 1.0f;
            rasterizer.cullMode = VkCullModeFlags.Back;
            rasterizer.frontFace = VkFrontFace.Clockwise;
            rasterizer.depthBiasEnable = VkBool32.False;

            var multisampling = new VkPipelineMultisampleStateCreateInfo();
            multisampling.sampleShadingEnable = VkBool32.False;
            multisampling.rasterizationSamples = VkSampleCountFlags.Count1;

            var colorBlendAttachment = new VkPipelineColorBlendAttachmentState();
            colorBlendAttachment.colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A;
            colorBlendAttachment.blendEnable = VkBool32.False;

            var colorBlending = new VkPipelineColorBlendStateCreateInfo();
            colorBlending.logicOpEnable = VkBool32.False;
            colorBlending.logicOp = VkLogicOp.Copy;
            colorBlending.attachmentCount = 1;
            colorBlending.pAttachments = &colorBlendAttachment;
            colorBlending.blendConstants[0] = 0.0f;
            colorBlending.blendConstants[1] = 0.0f;
            colorBlending.blendConstants[2] = 0.0f;
            colorBlending.blendConstants[3] = 0.0f;

            var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo();
            pipelineLayoutInfo.setLayoutCount = 0;
            pipelineLayoutInfo.pushConstantRangeCount = 0;

            VkPipelineLayout pipelineLayout;
            vkCreatePipelineLayout(VkDevice, &pipelineLayoutInfo, null, out pipelineLayout).CheckResult();

            VkPipelineShaderStageCreateInfo[] shaderStages = new VkPipelineShaderStageCreateInfo[] { vertShaderStageInfo, fragShaderStageInfo };
            fixed (VkPipelineShaderStageCreateInfo* shaderStagesPtr = &shaderStages[0])
            {
                fixed (VkFormat* pColorAttachmentFormat = &Swapchain.SurfaceFormat.format)
                {
                    VkPipelineRenderingCreateInfo pipelineRenderingCreateInfo = new VkPipelineRenderingCreateInfo
                    {
                        colorAttachmentCount = 1,
                        pColorAttachmentFormats = pColorAttachmentFormat
                    };

                    var pipelineInfo = new VkGraphicsPipelineCreateInfo();
                    pipelineInfo.pNext = &pipelineRenderingCreateInfo;
                    pipelineInfo.stageCount = 2;
                    pipelineInfo.pStages = shaderStagesPtr;
                    pipelineInfo.pVertexInputState = &vertexInputInfo;
                    pipelineInfo.pInputAssemblyState = &inputAssembly;
                    pipelineInfo.pViewportState = &viewportState;
                    pipelineInfo.pRasterizationState = &rasterizer;
                    pipelineInfo.pMultisampleState = &multisampling;
                    pipelineInfo.pColorBlendState = &colorBlending;
                    pipelineInfo.layout = pipelineLayout;
                    pipelineInfo.subpass = 0;
                    pipelineInfo.basePipelineHandle = VkPipeline.Null;

                    VkPipeline graphicsPipeline;
                    vkCreateGraphicsPipelines(VkDevice, VkPipelineCache.Null, 1, &pipelineInfo, null, &graphicsPipeline).CheckResult();
                    pipeline = graphicsPipeline;
                }
            }
        }

        vkDestroyShaderModule(VkDevice, fragShaderModule, null);
        vkDestroyShaderModule(VkDevice, vertShaderModule, null);
    }

    uint findMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
    {
        VkPhysicalDeviceMemoryProperties memProperties;
        vkGetPhysicalDeviceMemoryProperties(PhysicalDevice, out memProperties);

        for (int i = 0; i < memProperties.memoryTypeCount; i++)
        {
            if ((typeFilter & (1 << i)) != 0 && (memProperties.memoryTypes[i].propertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }

        throw new Exception("failed to find suitable memory type!");
    }

    private void CreateVertexBuffer()
    {
        var bufferSize = (uint)(Marshal.SizeOf<Vertex>() * Vertices.Length);

        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out stagingBuffer, out stagingBufferMemory);

        fixed (void* verticesPtr = &Vertices[0])
        {
            void* data;
            vkMapMemory(VkDevice, stagingBufferMemory, 0, bufferSize, 0, &data);
            Unsafe.CopyBlock(data, verticesPtr, bufferSize);
            //memcpy(data, vertices.data(), (size_t)bufferSize);
            vkUnmapMemory(VkDevice, stagingBufferMemory);
        }

        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferDst | VkBufferUsageFlags.VertexBuffer, VkMemoryPropertyFlags.DeviceLocal, out VertexBuffer, out VertexBufferMemory);

        CopyBuffer(stagingBuffer, VertexBuffer, bufferSize);

        vkDestroyBuffer(VkDevice, stagingBuffer, null);
        vkFreeMemory(VkDevice, stagingBufferMemory, null);
    }

    void CreateIndexBuffer()
    {
        uint bufferSize = (uint)(Marshal.SizeOf<short>() * Indices.Length);

        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out stagingBuffer, out stagingBufferMemory);

        fixed (ushort* indiciesPtr = &Indices[0])
        {
            void* data;
            vkMapMemory(VkDevice, stagingBufferMemory, 0, bufferSize, 0, &data);
            Unsafe.CopyBlock(data, indiciesPtr, bufferSize);
            vkUnmapMemory(VkDevice, stagingBufferMemory);
        }

        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferDst | VkBufferUsageFlags.IndexBuffer, VkMemoryPropertyFlags.DeviceLocal, out IndexBuffer, out IndexBufferMemory);

        CopyBuffer(stagingBuffer, IndexBuffer, bufferSize);

        vkDestroyBuffer(VkDevice, stagingBuffer, null);
        vkFreeMemory(VkDevice, stagingBufferMemory, null);
    }

    private void CreateBuffer(uint size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties, out VkBuffer buffer, out VkDeviceMemory bufferMemory)
    {
        VkBufferCreateInfo bufferInfo;
        bufferInfo.size = size;
        bufferInfo.usage = usage;
        bufferInfo.sharingMode = VkSharingMode.Exclusive;

        if (vkCreateBuffer(VkDevice, &bufferInfo, null, out buffer) != VkResult.Success)
        {
            throw new Exception("failed to create buffer!");
        }

        VkMemoryRequirements memRequirements;
        vkGetBufferMemoryRequirements(VkDevice, buffer, out memRequirements);

        VkMemoryAllocateInfo allocInfo;
        allocInfo.allocationSize = memRequirements.size;
        allocInfo.memoryTypeIndex = findMemoryType(memRequirements.memoryTypeBits, properties);

        if (vkAllocateMemory(VkDevice, &allocInfo, null, out bufferMemory) != VkResult.Success)
        {
            throw new Exception("failed to allocate buffer memory!");
        }

        vkBindBufferMemory(VkDevice, buffer, bufferMemory, 0);
    }

    void CopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, ulong size)
    {
        VkCommandBufferAllocateInfo allocInfo;
        allocInfo.level = VkCommandBufferLevel.Primary;
        allocInfo.commandPool = CommandPool;
        allocInfo.commandBufferCount = 1;

        VkCommandBuffer commandBuffer;
        vkAllocateCommandBuffer(VkDevice, &allocInfo, out commandBuffer);

        VkCommandBufferBeginInfo beginInfo;
        beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;

        vkBeginCommandBuffer(commandBuffer, &beginInfo);

        VkBufferCopy copyRegion;
        copyRegion.size = size;
        vkCmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);

        vkEndCommandBuffer(commandBuffer);

        VkSubmitInfo submitInfo;
        submitInfo.commandBufferCount = 1;
        submitInfo.pCommandBuffers = &commandBuffer;

        vkQueueSubmit(GraphicsQueue, 1, &submitInfo, VkFence.Null);
        vkQueueWaitIdle(GraphicsQueue);

        vkFreeCommandBuffers(VkDevice, CommandPool, 1, &commandBuffer);
    }

    private void CreateCommandPool()
    {
        // General Pool
        VkCommandPoolCreateInfo poolCreateInfo = new VkCommandPoolCreateInfo
        {
            flags = VkCommandPoolCreateFlags.Transient,
            queueFamilyIndex = queueFamilies.graphicsFamily,
        };
        vkCreateCommandPool(VkDevice, &poolCreateInfo, null, out CommandPool).CheckResult();

        // Per Frame Pools
        for (var i = 0; i < _perFrame.Length; i++)
        {
            poolCreateInfo = new VkCommandPoolCreateInfo
            {
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
            vkDeviceWaitIdle(VkDevice);
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
            vkCreateSemaphore(VkDevice, out _perFrame[CurrentSwapchainImageIndex].SwapchainReleaseSemaphore).CheckResult();
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
        vkQueueSubmit(GraphicsQueue, submitInfo, _perFrame[CurrentSwapchainImageIndex].QueueSubmitFence);

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
        GLFW.CreateWindowSurface(new VkHandle(VkInstance.Handle), window.WindowPtr, null, out var handle);
        return new VkSurfaceKHR((ulong)handle.Handle);
    }

    [UnmanagedCallersOnly]
    private static uint DebugMessengerCallback(VkDebugUtilsMessageSeverityFlagsEXT messageSeverity,
        VkDebugUtilsMessageTypeFlagsEXT messageTypes,
        VkDebugUtilsMessengerCallbackDataEXT* pCallbackData,
        void* userData)
    {
        string? message = VkStringInterop.ConvertToManaged(pCallbackData->pMessage);
        string prefix = messageTypes == VkDebugUtilsMessageTypeFlagsEXT.Validation ? "[Vulkan]: Validation: " : "[Vulkan]: ";

        switch (messageSeverity)
        {
            case VkDebugUtilsMessageSeverityFlagsEXT.Error:
                Log.Error($"{prefix}{messageSeverity} - {message}");
                if (messageTypes == VkDebugUtilsMessageTypeFlagsEXT.Validation)
                {
                    throw new Exception(message);
                }

                break;
            case VkDebugUtilsMessageSeverityFlagsEXT.Warning:
                Log.Warn($"{prefix}{messageSeverity} - {message}");
                break;
            case VkDebugUtilsMessageSeverityFlagsEXT.Info:
                Log.Info($"{prefix}{messageSeverity} - {message}");
                break;
            case VkDebugUtilsMessageSeverityFlagsEXT.Verbose:
                Log.Verbose($"{prefix}{messageSeverity} - {message}");
                break;
        }

        Debug.WriteLine($"{prefix}{messageSeverity} - {message}");

        return VK_FALSE;
    }

    private static void FindValidationLayers(List<string> appendTo)
    {
        ReadOnlySpan<VkLayerProperties> availableLayers = vkEnumerateInstanceLayerProperties();

        for (int j = 0; j < availableLayers.Length; j++)
        {
            var name = availableLayers[j].GetLayerName();
            Log.Info($"Found Layer: {name}");
        }

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

    #endregion

    private struct PerFrame
    {
        public VkImage Image;
        public VkImageView ImageView;
        public VkFence QueueSubmitFence;
        public VkCommandPool PrimaryCommandPool;
        public VkCommandBuffer PrimaryCommandBuffer;
        public VkSemaphore SwapchainAcquireSemaphore;
        public VkSemaphore SwapchainReleaseSemaphore;
    }
}