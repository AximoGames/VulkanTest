using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using Vortice.ShaderCompiler;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanPipelineBuilder : BackendPipelineBuilder
{
    private VulkanBufferManager _vulkanBufferManager;
    private readonly VulkanDevice _device;
    private readonly BackendDevice _backendDevice;
    private readonly VulkanSwapchainRenderTarget _swapchainRenderTarget;
    private readonly VulkanShaderManager _shaderManager;
    private VkPipeline PipelineHandle;
    private VkPipelineLayout PipelineLayoutHandle;
    private IDictionary<ShaderKind, VulkanShaderModule> _shaderModules = new Dictionary<ShaderKind, VulkanShaderModule>();
    private VertexLayoutInfo _vertexLayoutInfo;
    private PipelineLayoutDescription? _layoutDescription;
    private List<VkPushConstantRange> _pushConstantRanges = new List<VkPushConstantRange>();

    internal VulkanPipelineBuilder(VulkanDevice device, VulkanSwapchainRenderTarget swapchainRenderTarget, VulkanShaderManager shaderManager, VulkanBufferManager vulkanBufferManager)
    {
        _device = device;
        _swapchainRenderTarget = swapchainRenderTarget;
        _shaderManager = shaderManager;
        _vulkanBufferManager = vulkanBufferManager;
    }

    public override BackendBuffer CreateBuffer<T>(BufferType bufferType, int count)
        => _vulkanBufferManager.CreateBuffer<T>(bufferType, count);

    public override void CopyBuffer<T>(T[] source, int sourceStartIndex, BackendBuffer destinationBuffer, int destinationStartIndex, int count)
        => _vulkanBufferManager.CopyBuffer(source, 0, (VulkanBuffer)destinationBuffer, 0, count);

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

        var attributeDescriptions = stackalloc VkVertexInputAttributeDescription[_vertexLayoutInfo.AttributeDescriptions.Count];
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

        VkDescriptorSetLayout[] descriptorSetLayouts;
        var vertexInputInfo = new VkPipelineVertexInputStateCreateInfo
        {
            pVertexBindingDescriptions = &bindingDescription,
            vertexBindingDescriptionCount = 1,
            pVertexAttributeDescriptions = attributeDescriptions,
            vertexAttributeDescriptionCount = (uint)_vertexLayoutInfo.AttributeDescriptions.Count,
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
            width = _swapchainRenderTarget.Extent.X,
            height = _swapchainRenderTarget.Extent.Y,
            minDepth = 0.0f,
            maxDepth = 1.0f
        };

        var scissor = new VkRect2D
        {
            offset = new VkOffset2D(0, 0),
            extent = _swapchainRenderTarget.Extent.ToVkExtent2D(),
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

        if (_layoutDescription != null)
        {
            descriptorSetLayouts = new VkDescriptorSetLayout[_layoutDescription.DescriptorSetLayouts.Count];
            for (int i = 0; i < _layoutDescription.DescriptorSetLayouts.Count; i++)
                descriptorSetLayouts[i] = CreateDescriptorSetLayout(_layoutDescription.DescriptorSetLayouts[i]);
        }
        else
        {
            descriptorSetLayouts = [];
        }

        // if (descriptorSetLayouts.Length > 0 || _pushConstantRanges.Count > 0)
        // {
            fixed (VkDescriptorSetLayout* descriptorSetLayoutsPtr = descriptorSetLayouts)
            fixed (VkPushConstantRange* pushConstantRangesPtr = _pushConstantRanges.ToArray())
            {
                var pipelineLayoutInfo = new VkPipelineLayoutCreateInfo();
        
                if (descriptorSetLayouts.Length > 0)
                {
                    pipelineLayoutInfo.setLayoutCount = (uint)descriptorSetLayouts.Length;
                    pipelineLayoutInfo.pSetLayouts = descriptorSetLayoutsPtr;
                }
        
                if (_pushConstantRanges.Count > 0)
                {
                    pipelineLayoutInfo.pushConstantRangeCount = (uint)_pushConstantRanges.Count;
                    pipelineLayoutInfo.pPushConstantRanges = pushConstantRangesPtr;
                }
        
                vkCreatePipelineLayout(_device.LogicalDevice, &pipelineLayoutInfo, null, out PipelineLayoutHandle).CheckResult();
            }
        // }

        VkPipelineShaderStageCreateInfo* shaderStages = stackalloc VkPipelineShaderStageCreateInfo[] { vertShaderStageInfo, fragShaderStageInfo };

        var colorAttachmentFormat = _swapchainRenderTarget.SurfaceFormat.format;
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

        fragShaderModule.Free();
        vertShaderModule.Free();

        var pipelineLayoutHash = PipelineLayoutHandle.GetHashCode();
        return new VulkanPipeline(_device, PipelineHandle, PipelineLayoutHandle, pipelineLayoutHash, descriptorSetLayouts);
    }

    public override void ConfigureShader(string shaderCode, ShaderKind shaderKind)
    {
        _shaderModules.Add(shaderKind, _shaderManager.CreateShaderModuleFromCode(shaderCode, shaderKind));
    }

    public override void ConfigureVertexLayout(VertexLayoutInfo vertexLayoutInfo)
    {
        _vertexLayoutInfo = vertexLayoutInfo;
    }

    public override void ConfigurePipelineLayout(PipelineLayoutDescription layoutDescription)
    {
        _layoutDescription = layoutDescription;
    }

    public override void ConfigurePushConstants(uint size, ShaderStageFlags stageFlags)
    {
        var pushConstantRange = new VkPushConstantRange
        {
            stageFlags = ConvertShaderStageFlags(stageFlags),
            offset = 0,
            size = size
        };

        _pushConstantRanges.Add(pushConstantRange);
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

    private VkDescriptorSetLayout CreateDescriptorSetLayout(DescriptorSetLayoutDescription layoutDescription)
    {
        var bindings = new VkDescriptorSetLayoutBinding[layoutDescription.Bindings.Count];
        for (int i = 0; i < layoutDescription.Bindings.Count; i++)
        {
            bindings[i] = new VkDescriptorSetLayoutBinding
            {
                binding = layoutDescription.Bindings[i].Binding,
                descriptorType = ConvertDescriptorType(layoutDescription.Bindings[i].DescriptorType),
                descriptorCount = layoutDescription.Bindings[i].DescriptorCount,
                stageFlags = ConvertShaderStageFlags(layoutDescription.Bindings[i].StageFlags)
            };
        }

        VkDescriptorBindingFlags* bindingFlags = stackalloc VkDescriptorBindingFlags[]
        {
            VkDescriptorBindingFlags.PartiallyBound
            // | VkDescriptorBindingFlags.UpdateAfterBind 
        };
        VkDescriptorSetLayoutBindingFlagsCreateInfo bindingFlagsInfo = new VkDescriptorSetLayoutBindingFlagsCreateInfo
        {
            bindingCount = (uint)bindings.Length,
            pBindingFlags = bindingFlags,
        };

        fixed (VkDescriptorSetLayoutBinding* bindingsPtr = bindings)
        {
            VkDescriptorSetLayoutCreateInfo layoutInfo = new VkDescriptorSetLayoutCreateInfo
            {
                pNext = &bindingFlagsInfo,
                flags = VkDescriptorSetLayoutCreateFlags.UpdateAfterBindPool,
                bindingCount = (uint)bindings.Length,
                pBindings = bindingsPtr
            };

            VkDescriptorSetLayout descriptorSetLayout;
            vkCreateDescriptorSetLayout(_device.LogicalDevice, &layoutInfo, null, out descriptorSetLayout).CheckResult();
            return descriptorSetLayout;
        }
    }

    private VkDescriptorType ConvertDescriptorType(DescriptorType descriptorType)
    {
        return descriptorType switch
        {
            DescriptorType.UniformBuffer => VkDescriptorType.UniformBuffer,
            DescriptorType.UniformBufferDynamic => VkDescriptorType.UniformBufferDynamic,
            DescriptorType.StorageBuffer => VkDescriptorType.StorageBuffer,
            DescriptorType.CombinedImageSampler => VkDescriptorType.CombinedImageSampler,
            // Add more cases as needed
            _ => throw new ArgumentException("Unsupported descriptor type")
        };
    }

    private VkShaderStageFlags ConvertShaderStageFlags(ShaderStageFlags stageFlags)
    {
        VkShaderStageFlags result = VkShaderStageFlags.None;
        if ((stageFlags & ShaderStageFlags.Vertex) != 0) result |= VkShaderStageFlags.Vertex;
        if ((stageFlags & ShaderStageFlags.Fragment) != 0) result |= VkShaderStageFlags.Fragment;
        if ((stageFlags & ShaderStageFlags.Compute) != 0) result |= VkShaderStageFlags.Compute;
        // Add more cases as needed
        return result;
    }
}