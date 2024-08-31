using System.Collections.Generic;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using OpenTK.Mathematics;
using Vortice.ShaderCompiler;

namespace VulkanTest;

public unsafe class PipelineBuilder
{
    private readonly VulkanDevice _device;
    private readonly Swapchain _swapchain;
    private readonly ShaderManager _shaderManager;
    private VkPipeline PipelineHandle;
    private VkPipelineLayout PipelineLayoutHandle;
    private IDictionary<ShaderKind, ShaderModule> _shaderModules = new Dictionary<ShaderKind, ShaderModule>();

    internal PipelineBuilder(VulkanDevice device, Swapchain swapchain, ShaderManager shaderManager)
    {
        _device = device;
        _swapchain = swapchain;
        _shaderManager = shaderManager;
    }

    internal VulkanPipeline Build()
    {
        ShaderModule vertShaderModule = _shaderModules[ShaderKind.VertexShader];
        ShaderModule fragShaderModule = _shaderModules[ShaderKind.FragmentShader];

        var name = "main".ToVkUtf8ReadOnlyString();

        var vertShaderStageInfo = new VkPipelineShaderStageCreateInfo
        {
            stage = VkShaderStageFlags.Vertex,
            module = vertShaderModule.Module,
            pName = name
        };

        var fragShaderStageInfo = new VkPipelineShaderStageCreateInfo
        {
            stage = VkShaderStageFlags.Fragment,
            module = fragShaderModule.Module,
            pName = name
        };

        var bindingDescription = Vertex.GetBindingDescription();
        var attributeDescriptions = Vertex.GetAttributeDescriptions();

        fixed (VkVertexInputAttributeDescription* attributeDescriptionsPtr = &attributeDescriptions[0])
        {
            var vertexInputInfo = new VkPipelineVertexInputStateCreateInfo
            {
                vertexBindingDescriptionCount = 1,
                vertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                pVertexBindingDescriptions = &bindingDescription,
                pVertexAttributeDescriptions = attributeDescriptionsPtr
            };

            var inputAssembly = new VkPipelineInputAssemblyStateCreateInfo
            {
                topology = VkPrimitiveTopology.TriangleList,
                primitiveRestartEnable = VkBool32.False
            };

            var viewport = new VkViewport
            {
                x = 0.0f,
                y = 0.0f,
                width = _swapchain.Extent.width,
                height = _swapchain.Extent.height,
                minDepth = 0.0f,
                maxDepth = 1.0f
            };

            var scissor = new VkRect2D
            {
                offset = new VkOffset2D(0, 0),
                extent = _swapchain.Extent
            };

            var viewportState = new VkPipelineViewportStateCreateInfo
            {
                viewportCount = 1,
                pViewports = &viewport,
                scissorCount = 1,
                pScissors = &scissor
            };

            var rasterizer = new VkPipelineRasterizationStateCreateInfo
            {
                depthClampEnable = VkBool32.False,
                rasterizerDiscardEnable = VkBool32.False,
                polygonMode = VkPolygonMode.Fill,
                lineWidth = 1.0f,
                cullMode = VkCullModeFlags.Back,
                frontFace = VkFrontFace.Clockwise,
                depthBiasEnable = VkBool32.False
            };

            var multisampling = new VkPipelineMultisampleStateCreateInfo
            {
                sampleShadingEnable = VkBool32.False,
                rasterizationSamples = VkSampleCountFlags.Count1
            };

            var colorBlendAttachment = new VkPipelineColorBlendAttachmentState
            {
                colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A,
                blendEnable = VkBool32.False
            };

            var colorBlending = new VkPipelineColorBlendStateCreateInfo
            {
                logicOpEnable = VkBool32.False,
                logicOp = VkLogicOp.Copy,
                attachmentCount = 1,
                pAttachments = &colorBlendAttachment,
            };
            colorBlending.blendConstants[0] = 0.0f;
            colorBlending.blendConstants[1] = 0.0f;
            colorBlending.blendConstants[2] = 0.0f;
            colorBlending.blendConstants[3] = 0.0f;

            var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo
            {
                setLayoutCount = 0,
                pushConstantRangeCount = 0
            };

            vkCreatePipelineLayout(_device.LogicalDevice, &pipelineLayoutInfo, null, out PipelineLayoutHandle).CheckResult();

            VkPipelineShaderStageCreateInfo* shaderStages = stackalloc VkPipelineShaderStageCreateInfo[] { vertShaderStageInfo, fragShaderStageInfo };

            var colorAttachmentFormat = _swapchain.SurfaceFormat.format;
            VkPipelineRenderingCreateInfo pipelineRenderingCreateInfo = new VkPipelineRenderingCreateInfo
            {
                colorAttachmentCount = 1,
                pColorAttachmentFormats = &colorAttachmentFormat,
            };

            var pipelineInfo = new VkGraphicsPipelineCreateInfo
            {
                pNext = &pipelineRenderingCreateInfo,
                stageCount = 2,
                pStages = shaderStages,
                pVertexInputState = &vertexInputInfo,
                pInputAssemblyState = &inputAssembly,
                pViewportState = &viewportState,
                pRasterizationState = &rasterizer,
                pMultisampleState = &multisampling,
                pColorBlendState = &colorBlending,
                layout = PipelineLayoutHandle,
                subpass = 0,
                basePipelineHandle = VkPipeline.Null
            };

            VkPipeline graphicsPipeline;
            vkCreateGraphicsPipelines(_device.LogicalDevice, VkPipelineCache.Null, 1, &pipelineInfo, null, &graphicsPipeline).CheckResult();
            PipelineHandle = graphicsPipeline;
        }

        fragShaderModule.Free();
        vertShaderModule.Free();

        return new VulkanPipeline(_device, PipelineHandle, PipelineLayoutHandle);
    }
    
    public void ConfigureShader(string shaderCode, ShaderKind shaderKind)
    {
        _shaderModules.Add(shaderKind, _shaderManager.CreateShaderModuleFromCode(shaderCode, shaderKind));
    }
}