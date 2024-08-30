using System;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using OpenTK.Mathematics;
using Vortice.ShaderCompiler;

namespace Vortice;

public unsafe class VulkanPipeline : IDisposable
{
    private readonly VulkanDevice _device;
    private readonly Swapchain _swapchain;
    private readonly ShaderManager _shaderManager;

    public VkPipeline Handle;
    public VkPipelineLayout Layout;

    public VulkanPipeline(VulkanDevice device, Swapchain swapchain, ShaderManager shaderManager)
    {
        _device = device;
        _swapchain = swapchain;
        _shaderManager = shaderManager;
        CreateGraphicsPipeline();
    }

    private void CreateGraphicsPipeline()
    {
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

        string fragShaderCode =
            """
            #version 450

            layout(location = 0) in vec3 fragColor;

            layout(location = 0) out vec4 outColor;

            void main() {
                outColor = vec4(fragColor, 1.0);
            }
            """;

        VkShaderModule vertShaderModule = _shaderManager.CreateShaderModuleFromCode(vertexShaderCode, ShaderKind.VertexShader);
        VkShaderModule fragShaderModule = _shaderManager.CreateShaderModuleFromCode(fragShaderCode, ShaderKind.FragmentShader);

        var name = "main".ToVkUtf8ReadOnlyString();

        var vertShaderStageInfo = new VkPipelineShaderStageCreateInfo
        {
            stage = VkShaderStageFlags.Vertex,
            module = vertShaderModule,
            pName = name
        };

        var fragShaderStageInfo = new VkPipelineShaderStageCreateInfo
        {
            stage = VkShaderStageFlags.Fragment,
            module = fragShaderModule,
            pName = name
        };

        var bindingDescription = Vertex.getBindingDescription();
        var attributeDescriptions = Vertex.getAttributeDescriptions();

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

            vkCreatePipelineLayout(_device.LogicalDevice, &pipelineLayoutInfo, null, out Layout).CheckResult();

            VkPipelineShaderStageCreateInfo[] shaderStages = new VkPipelineShaderStageCreateInfo[] { vertShaderStageInfo, fragShaderStageInfo };
            fixed (VkPipelineShaderStageCreateInfo* shaderStagesPtr = &shaderStages[0])
            {
                fixed (VkFormat* pColorAttachmentFormat = &_swapchain.SurfaceFormat.format)
                {
                    VkPipelineRenderingCreateInfo pipelineRenderingCreateInfo = new VkPipelineRenderingCreateInfo
                    {
                        colorAttachmentCount = 1,
                        pColorAttachmentFormats = pColorAttachmentFormat
                    };

                    var pipelineInfo = new VkGraphicsPipelineCreateInfo
                    {
                        pNext = &pipelineRenderingCreateInfo,
                        stageCount = 2,
                        pStages = shaderStagesPtr,
                        pVertexInputState = &vertexInputInfo,
                        pInputAssemblyState = &inputAssembly,
                        pViewportState = &viewportState,
                        pRasterizationState = &rasterizer,
                        pMultisampleState = &multisampling,
                        pColorBlendState = &colorBlending,
                        layout = Layout,
                        subpass = 0,
                        basePipelineHandle = VkPipeline.Null
                    };

                    VkPipeline graphicsPipeline;
                    vkCreateGraphicsPipelines(_device.LogicalDevice, VkPipelineCache.Null, 1, &pipelineInfo, null, &graphicsPipeline).CheckResult();
                    Handle = graphicsPipeline;
                }
            }
        }

        vkDestroyShaderModule(_device.LogicalDevice, fragShaderModule, null);
        vkDestroyShaderModule(_device.LogicalDevice, vertShaderModule, null);
    }

    public void Dispose()
    {
        if (Handle != VkPipeline.Null)
        {
            vkDestroyPipeline(_device.LogicalDevice, Handle, null);
        }

        if (Layout != VkPipelineLayout.Null)
        {
            vkDestroyPipelineLayout(_device.LogicalDevice, Layout, null);
        }
    }
}