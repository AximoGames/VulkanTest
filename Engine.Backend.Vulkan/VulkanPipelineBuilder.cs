using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using Vortice.ShaderCompiler;
using Vortice.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanBackendPipelineBuilder : BackendPipelineBuilder
{
    private VulkanBufferManager _vulkanBufferManager;
    private readonly VulkanDevice _device;
    private readonly VulkanSwapchain _swapchain;
    private readonly VulkanShaderManager _shaderManager;
    private VkPipeline PipelineHandle;
    private VkPipelineLayout PipelineLayoutHandle;
    private IDictionary<ShaderKind, VulkanShaderModule> _shaderModules = new Dictionary<ShaderKind, VulkanShaderModule>();
    private VertexLayoutInfo _vertexLayoutInfo;

    internal VulkanBackendPipelineBuilder(VulkanDevice device, VulkanSwapchain swapchain, VulkanShaderManager shaderManager, VulkanBufferManager vulkanBufferManager)
    {
        _device = device;
        _swapchain = swapchain;
        _shaderManager = shaderManager;
        _vulkanBufferManager = vulkanBufferManager;
    }

    public override BackendBuffer CreateVertexBuffer<T>(T[] vertices)
        => _vulkanBufferManager.CreateVertexBuffer(vertices);

    public override BackendBuffer CreateIndexBuffer<T>(T[] indices)
        => _vulkanBufferManager.CreateIndexBuffer(indices);

    public override BackendPipeline Build()
    {
        VulkanShaderModule vertShaderModule = _shaderModules[ShaderKind.VertexShader];
        VulkanShaderModule fragShaderModule = _shaderModules[ShaderKind.FragmentShader];

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

        // Convert VertexLayoutInfo to Vulkan structures
        VkVertexInputBindingDescription bindingDescription = new VkVertexInputBindingDescription
        {
            binding = _vertexLayoutInfo.BindingDescription.Binding,
            stride = _vertexLayoutInfo.BindingDescription.Stride,
            inputRate = ConvertInputRate(_vertexLayoutInfo.BindingDescription.InputRate)
        };

        var attributeDescriptions = new VkVertexInputAttributeDescription[_vertexLayoutInfo.AttributeDescriptions.Count];
        for (int i = 0; i < _vertexLayoutInfo.AttributeDescriptions.Count; i++)
        {
            attributeDescriptions[i] = new VkVertexInputAttributeDescription
            {
                binding = _vertexLayoutInfo.AttributeDescriptions[i].Binding,
                location = _vertexLayoutInfo.AttributeDescriptions[i].Location,
                format = ConvertFormat(_vertexLayoutInfo.AttributeDescriptions[i].Format),
                offset = _vertexLayoutInfo.AttributeDescriptions[i].Offset
            };
        }

        fixed (VkVertexInputAttributeDescription* attributeDescriptionsPtr = &attributeDescriptions[0])
        {
            var vertexInputInfo = new VkPipelineVertexInputStateCreateInfo
            {
                vertexBindingDescriptionCount = 1,
                pVertexBindingDescriptions = &bindingDescription,
                vertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
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
                width = _swapchain.Extent.X,
                height = _swapchain.Extent.Y,
                minDepth = 0.0f,
                maxDepth = 1.0f
            };

            var scissor = new VkRect2D
            {
                offset = new VkOffset2D(0, 0),
                extent = _swapchain.Extent.ToVkExtent2D(),
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

            Vortice.Vulkan.Vulkan.vkCreatePipelineLayout(_device.LogicalDevice, &pipelineLayoutInfo, null, out PipelineLayoutHandle).CheckResult();

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
            Vortice.Vulkan.Vulkan.vkCreateGraphicsPipelines(_device.LogicalDevice, VkPipelineCache.Null, 1, &pipelineInfo, null, &graphicsPipeline).CheckResult();
            PipelineHandle = graphicsPipeline;
        }

        fragShaderModule.Free();
        vertShaderModule.Free();

        return new VulkanPipeline(_device, PipelineHandle, PipelineLayoutHandle);
    }

    public override void ConfigureShader(string shaderCode, ShaderKind shaderKind)
    {
        _shaderModules.Add(shaderKind, _shaderManager.CreateShaderModuleFromCode(shaderCode, shaderKind));
    }

    public override void ConfigureVertexLayout(VertexLayoutInfo vertexLayoutInfo)
    {
        _vertexLayoutInfo = vertexLayoutInfo;
    }

    private VkVertexInputRate ConvertInputRate(VertexInputRate inputRate)
    {
        return inputRate switch
        {
            VertexInputRate.Vertex => VkVertexInputRate.Vertex,
            VertexInputRate.Instance => VkVertexInputRate.Instance,
            _ => throw new ArgumentOutOfRangeException(nameof(inputRate))
        };
    }

    private VkFormat ConvertFormat(VertexFormat format)
    {
        return format switch
        {
            VertexFormat.Float32_2 => VkFormat.R32G32Sfloat,
            VertexFormat.Float32_3 => VkFormat.R32G32B32Sfloat,
            VertexFormat.Float32_4 => VkFormat.R32G32B32A32Sfloat,
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }
}