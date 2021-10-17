using System;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Windowing.Desktop;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Vortice
{

    public sealed unsafe class Swapchain : IDisposable
    {
        public readonly GraphicsDevice Device;
        [NotNull]
        public readonly GameWindow? Window = default!;
        public VkSwapchainKHR Handle;
        public VkExtent2D Extent { get; }

        public VkSurfaceFormatKHR SurfaceFormat;

        public Swapchain(GraphicsDevice device, GameWindow? window)
        {
            Device = device;
            Window = window;

            SwapChainSupportDetails swapChainSupport = QuerySwapChainSupport(device.PhysicalDevice, device._surface);

            SurfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
            VkPresentModeKHR presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
            Extent = ChooseSwapExtent(swapChainSupport.Capabilities);

            uint imageCount = swapChainSupport.Capabilities.minImageCount + 1;
            if (swapChainSupport.Capabilities.maxImageCount > 0 &&
                imageCount > swapChainSupport.Capabilities.maxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.maxImageCount;
            }

            var createInfo = new VkSwapchainCreateInfoKHR
            {
                sType = VkStructureType.SwapchainCreateInfoKHR,
                surface = device._surface,
                minImageCount = imageCount,
                imageFormat = SurfaceFormat.format,
                imageColorSpace = SurfaceFormat.colorSpace,
                imageExtent = Extent,
                imageArrayLayers = 1,
                imageUsage = VkImageUsageFlags.ColorAttachment,
                imageSharingMode = VkSharingMode.Exclusive,
                preTransform = swapChainSupport.Capabilities.currentTransform,
                compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
                presentMode = presentMode,
                clipped = true,
                oldSwapchain = VkSwapchainKHR.Null
            };

            vkCreateSwapchainKHR(device.VkDevice, &createInfo, null, out Handle).CheckResult();
        }

        public void Dispose()
        {
            //for (int i = 0; i < _swapChainImageViews.Length; i++)
            //{
            //    vkDestroyImageView(Device, _swapChainImageViews[i], null);
            //}

            //for (int i = 0; i < Framebuffers.Length; i++)
            //{
            //    vkDestroyFramebuffer(Device, Framebuffers[i], null);
            //}

            //vkDestroyRenderPass(Device, RenderPass, null);

            //if (Handle != VkSwapchainKHR.Null)
            //{
            //    vkDestroySwapchainKHR(Device, Handle, null);
            //}
        }

        //void createGraphicsPipeline()
        //{
        //    auto vertShaderCode = readFile("shaders/vert.spv");
        //    auto fragShaderCode = readFile("shaders/frag.spv");

        //    VkShaderModule vertShaderModule = createShaderModule(vertShaderCode);
        //    VkShaderModule fragShaderModule = createShaderModule(fragShaderCode);

        //    using var name = new VkString("main");

        //    var vertShaderStageInfo = new VkPipelineShaderStageCreateInfo();
        //    vertShaderStageInfo.sType = VkStructureType.PipelineShaderStageCreateInfo;
        //    vertShaderStageInfo.stage = VkShaderStageFlags.Vertex;
        //    vertShaderStageInfo.module = vertShaderModule;
        //    vertShaderStageInfo.pName = name.Pointer;

        //    var fragShaderStageInfo = new VkPipelineShaderStageCreateInfo();
        //    fragShaderStageInfo.sType = VkStructureType.PipelineShaderStageCreateInfo;
        //    fragShaderStageInfo.stage = VkShaderStageFlags.Fragment;
        //    fragShaderStageInfo.module = fragShaderModule;
        //    fragShaderStageInfo.pName = name.Pointer;

        //    VkPipelineShaderStageCreateInfo[] shaderStages = new VkPipelineShaderStageCreateInfo[] { vertShaderStageInfo, fragShaderStageInfo };

        //    var vertexInputInfo = new VkPipelineVertexInputStateCreateInfo();
        //    vertexInputInfo.sType = VkStructureType.PipelineVertexInputStateCreateInfo;
        //    vertexInputInfo.vertexBindingDescriptionCount = 0;
        //    vertexInputInfo.vertexAttributeDescriptionCount = 0;

        //    var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo();
        //    inputAssembly.sType = VkStructureType.PipelineInputAssemblyStateCreateInfo;
        //    inputAssembly.topology = VkPrimitiveTopology.TriangleList;
        //    inputAssembly.primitiveRestartEnable = VkBool32.False;

        //    var viewport = new VkViewport();
        //    viewport.x = 0.0f;
        //    viewport.y = 0.0f;
        //    viewport.width = Extent.width;
        //    viewport.height = Extent.height;
        //    viewport.minDepth = 0.0f;
        //    viewport.maxDepth = 1.0f;

        //    var scissor = new VkRect2D();
        //    scissor.offset = new VkOffset2D(0, 0);
        //    scissor.extent = Extent;

        //    var viewportState = new VkPipelineViewportStateCreateInfo();
        //    viewportState.sType = VkStructureType.PipelineViewportStateCreateInfo;
        //    viewportState.viewportCount = 1;
        //    viewportState.pViewports = &viewport;
        //    viewportState.scissorCount = 1;
        //    viewportState.pScissors = &scissor;

        //    var rasterizer = new VkPipelineRasterizationStateCreateInfo();
        //    rasterizer.sType = VkStructureType.PipelineRasterizationStateCreateInfo;
        //    rasterizer.depthClampEnable = VkBool32.False;
        //    rasterizer.rasterizerDiscardEnable = VkBool32.False;
        //    rasterizer.polygonMode = VkPolygonMode.Fill;
        //    rasterizer.lineWidth = 1.0f;
        //    rasterizer.cullMode = VkCullModeFlags.Back;
        //    rasterizer.frontFace = VkFrontFace.Clockwise;
        //    rasterizer.depthBiasEnable = VkBool32.False;

        //    var multisampling = new VkPipelineMultisampleStateCreateInfo();
        //    multisampling.sType = VkStructureType.PipelineMultisampleStateCreateInfo;
        //    multisampling.sampleShadingEnable = VkBool32.False;
        //    multisampling.rasterizationSamples = VkSampleCountFlags.Count1;

        //    var colorBlendAttachment = new VkPipelineColorBlendAttachmentState();
        //    colorBlendAttachment.colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A;
        //    colorBlendAttachment.blendEnable = VkBool32.False;

        //    var colorBlending = new VkPipelineColorBlendStateCreateInfo();
        //    colorBlending.sType = VkStructureType.PipelineColorBlendStateCreateInfo;
        //    colorBlending.logicOpEnable = VkBool32.False;
        //    colorBlending.logicOp = VkLogicOp.Copy;
        //    colorBlending.attachmentCount = 1;
        //    colorBlending.pAttachments = &colorBlendAttachment;
        //    colorBlending.blendConstants[0] = 0.0f;
        //    colorBlending.blendConstants[1] = 0.0f;
        //    colorBlending.blendConstants[2] = 0.0f;
        //    colorBlending.blendConstants[3] = 0.0f;

        //    var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo();
        //    pipelineLayoutInfo.sType = VkStructureType.PipelineLayoutCreateInfo;
        //    pipelineLayoutInfo.setLayoutCount = 0;
        //    pipelineLayoutInfo.pushConstantRangeCount = 0;

        //    VkPipelineLayout pipelineLayout;
        //    vkCreatePipelineLayout(Device, &pipelineLayoutInfo, null, out pipelineLayout).CheckResult();

        //    var pipelineInfo = new VkGraphicsPipelineCreateInfo();
        //    pipelineInfo.sType = VkStructureType.GraphicsPipelineCreateInfo;
        //    pipelineInfo.stageCount = 2;
        //    pipelineInfo.pStages = shaderStages;
        //    pipelineInfo.pVertexInputState = &vertexInputInfo;
        //    pipelineInfo.pInputAssemblyState = &inputAssembly;
        //    pipelineInfo.pViewportState = &viewportState;
        //    pipelineInfo.pRasterizationState = &rasterizer;
        //    pipelineInfo.pMultisampleState = &multisampling;
        //    pipelineInfo.pColorBlendState = &colorBlending;
        //    pipelineInfo.layout = pipelineLayout;
        //    pipelineInfo.renderPass = RenderPass;
        //    pipelineInfo.subpass = 0;
        //    pipelineInfo.basePipelineHandle = VkPipeline.Null;

        //    VkPipeline graphicsPipeline;
        //    vkCreateGraphicsPipelines(Device, VkPipelineCache.Null, 1, &pipelineInfo, null, &graphicsPipeline).CheckResult();

        //    vkDestroyShaderModule(Device, fragShaderModule, null);
        //    vkDestroyShaderModule(Device, vertShaderModule, null);
        //}

        private ref struct SwapChainSupportDetails
        {
            public VkSurfaceCapabilitiesKHR Capabilities;
            public ReadOnlySpan<VkSurfaceFormatKHR> Formats;
            public ReadOnlySpan<VkPresentModeKHR> PresentModes;
        };

        private VkExtent2D ChooseSwapExtent(VkSurfaceCapabilitiesKHR capabilities)
        {
            if (capabilities.currentExtent.width > 0)
            {
                return capabilities.currentExtent;
            }
            else
            {
                VkExtent2D actualExtent = new VkExtent2D(Window.ClientSize.X, Window.ClientSize.Y);

                actualExtent = new VkExtent2D(
                    Math.Max(capabilities.minImageExtent.width, Math.Min(capabilities.maxImageExtent.width, actualExtent.width)),
                    Math.Max(capabilities.minImageExtent.height, Math.Min(capabilities.maxImageExtent.height, actualExtent.height))
                    );

                return actualExtent;
            }
        }

        private static SwapChainSupportDetails QuerySwapChainSupport(VkPhysicalDevice device, VkSurfaceKHR surface)
        {
            SwapChainSupportDetails details = new SwapChainSupportDetails();
            vkGetPhysicalDeviceSurfaceCapabilitiesKHR(device, surface, out details.Capabilities).CheckResult();

            details.Formats = vkGetPhysicalDeviceSurfaceFormatsKHR(device, surface);
            details.PresentModes = vkGetPhysicalDeviceSurfacePresentModesKHR(device, surface);
            return details;
        }

        private static VkSurfaceFormatKHR ChooseSwapSurfaceFormat(ReadOnlySpan<VkSurfaceFormatKHR> availableFormats)
        {
            // If the surface format list only includes one entry with VK_FORMAT_UNDEFINED,
            // there is no preferred format, so we assume VK_FORMAT_B8G8R8A8_UNORM
            if ((availableFormats.Length == 1) && (availableFormats[0].format == VkFormat.Undefined))
            {
                return new VkSurfaceFormatKHR(VkFormat.B8G8R8A8UNorm, availableFormats[0].colorSpace);
            }

            // iterate over the list of available surface format and
            // check for the presence of VK_FORMAT_B8G8R8A8_UNORM
            foreach (VkSurfaceFormatKHR availableFormat in availableFormats)
            {
                if (availableFormat.format == VkFormat.B8G8R8A8UNorm)
                {
                    return availableFormat;
                }
            }

            return availableFormats[0];
        }

        private static VkPresentModeKHR ChooseSwapPresentMode(ReadOnlySpan<VkPresentModeKHR> availablePresentModes)
        {
            foreach (VkPresentModeKHR availablePresentMode in availablePresentModes)
            {
                if (availablePresentMode == VkPresentModeKHR.Mailbox)
                {
                    return availablePresentMode;
                }
            }

            return VkPresentModeKHR.Fifo;
        }
    }
}