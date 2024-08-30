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
    public readonly VkPipeline Pipeline;
    private PerFrame[] _perFrame; // TODO: Pin during init?
    public readonly BufferManager BufferManager;
    public VkCommandPool CommandPool;

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

        CreateGraphicsPipeline(out Pipeline);

        CreateCommandPool();

        BufferManager = new BufferManager(VulkanDevice, CommandPool);
        BufferManager.CreateVertexBuffer(Vertices);
        BufferManager.CreateIndexBuffer(Indices);

        for (var i = 0; i < _perFrame.Length; i++)
        {
            VkFenceCreateInfo fenceCreateInfo = new VkFenceCreateInfo(VkFenceCreateFlags.Signaled);
            vkCreateFence(VulkanDevice.LogicalDevice, &fenceCreateInfo, null, out _perFrame[i].QueueSubmitFence).CheckResult();

            vkAllocateCommandBuffer(VulkanDevice.LogicalDevice, _perFrame[i].PrimaryCommandPool, out _perFrame[i].PrimaryCommandBuffer).CheckResult();
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

        VkShaderModule vertShaderModule = ShaderManager.CreateShaderModuleFromCode(vertexShaderCode, ShaderKind.VertexShader);
        VkShaderModule fragShaderModule = ShaderManager.CreateShaderModuleFromCode(fragShaderCode, ShaderKind.FragmentShader);

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
            vkCreatePipelineLayout(VulkanDevice.LogicalDevice, &pipelineLayoutInfo, null, out pipelineLayout).CheckResult();

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
                    vkCreateGraphicsPipelines(VulkanDevice.LogicalDevice, VkPipelineCache.Null, 1, &pipelineInfo, null, &graphicsPipeline).CheckResult();
                    pipeline = graphicsPipeline;
                }
            }
        }

        vkDestroyShaderModule(VulkanDevice.LogicalDevice, fragShaderModule, null);
        vkDestroyShaderModule(VulkanDevice.LogicalDevice, vertShaderModule, null);
    }

    private void CreateCommandPool()
    {
        // General Pool
        VkCommandPoolCreateInfo poolCreateInfo = new VkCommandPoolCreateInfo
        {
            flags = VkCommandPoolCreateFlags.Transient,
            queueFamilyIndex = VulkanDevice.QueueFamilies.graphicsFamily,
        };
        vkCreateCommandPool(VulkanDevice.LogicalDevice, &poolCreateInfo, null, out CommandPool).CheckResult();

        // Per Frame Pools
        for (var i = 0; i < _perFrame.Length; i++)
        {
            poolCreateInfo = new VkCommandPoolCreateInfo
            {
                flags = VkCommandPoolCreateFlags.Transient,
                queueFamilyIndex = VulkanDevice.QueueFamilies.graphicsFamily,
            };
            vkCreateCommandPool(VulkanDevice.LogicalDevice, &poolCreateInfo, null, out _perFrame[i].PrimaryCommandPool).CheckResult();
        }
    }

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
                vkFreeCommandBuffers(VulkanDevice.LogicalDevice, _perFrame[i].PrimaryCommandPool, _perFrame[i].PrimaryCommandBuffer);

                _perFrame[i].PrimaryCommandBuffer = IntPtr.Zero;
            }

            vkDestroyCommandPool(VulkanDevice.LogicalDevice, _perFrame[i].PrimaryCommandPool, null);

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

        if (_perFrame[imageIndex].PrimaryCommandPool != VkCommandPool.Null)
        {
            vkResetCommandPool(VulkanDevice.LogicalDevice, _perFrame[imageIndex].PrimaryCommandPool, VkCommandPoolResetFlags.None);
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
        public VkCommandPool PrimaryCommandPool;
        public VkCommandBuffer PrimaryCommandBuffer;
        public VkSemaphore SwapchainAcquireSemaphore;
        public VkSemaphore SwapchainReleaseSemaphore;
    }
}