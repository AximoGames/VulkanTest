using OpenTK;
using OpenTK.Mathematics;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanRenderPipelineContext : BackendRenderContext
{
    private readonly VulkanDevice _device;
    private readonly VkCommandBuffer _commandBuffer;
    private readonly Vector2i _extent;
    private VulkanPipeline _pipeline;

    internal VulkanRenderPipelineContext(BackendUsePassContext passContext, VulkanDevice device, VkCommandBuffer commandBuffer, Vector2i extent, VulkanPipeline pipeline)
    {
        _device = device;
        _commandBuffer = commandBuffer;
        _extent = extent;
        _pipeline = pipeline;
        PassContext = passContext;
    }

    public override BackendUsePassContext PassContext { get; }

    public override void BindVertexBuffer(BackendBuffer backendBuffer, uint binding = 0)
        => vkCmdBindVertexBuffer(_commandBuffer, binding, ((VulkanBuffer)backendBuffer).Buffer);

    public override void BindIndexBuffer(BackendBuffer backendBuffer)
        => vkCmdBindIndexBuffer(_commandBuffer, ((VulkanBuffer)backendBuffer).Buffer, 0, backendBuffer.ElementType.ToVkIndexType());

    public override void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        => vkCmdDrawIndexed(_commandBuffer, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);

    public override void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
        => vkCmdDraw(_commandBuffer, vertexCount, instanceCount, firstVertex, firstInstance);

    public override void BindUniformBuffer(BackendBuffer buffer, uint set, uint binding, Span<uint> dynamicOffsets)
    {
        VulkanBuffer vulkanBuffer = (VulkanBuffer)buffer;

        VkDescriptorSet descriptorSet = _device.DescriptorSetManager.GetOrAllocateDescriptorSet(_pipeline.PipelineLayout, set, _pipeline.DescriptorSetLayouts[set]);
        _device.DescriptorSetManager.UpdateDescriptorSet(descriptorSet, binding, vulkanBuffer);

        if (dynamicOffsets.Length == 0)
        {
            vkCmdBindDescriptorSets(
                commandBuffer: _commandBuffer,
                pipelineBindPoint: VkPipelineBindPoint.Graphics,
                layout: _pipeline.PipelineLayout,
                firstSet: set,
                descriptorSetCount: 1,
                descriptorSets: &descriptorSet,
                dynamicOffsetCount: 0,
                dynamicOffsets: null);
        }
        else
        {
            fixed (uint* dynamicOffsetPtr = dynamicOffsets)
            {
                vkCmdBindDescriptorSets(
                    commandBuffer: _commandBuffer,
                    pipelineBindPoint: VkPipelineBindPoint.Graphics,
                    layout: _pipeline.PipelineLayout,
                    firstSet: set,
                    descriptorSetCount: 1,
                    descriptorSets: &descriptorSet,
                    dynamicOffsetCount: (uint)dynamicOffsets.Length,
                    dynamicOffsets: dynamicOffsetPtr);
            }
        }
    }

    public override void SetPushConstants<T>(ShaderStageFlags stageFlags, uint offset, T[] data)
    {
        fixed (void* pData = data)
            vkCmdPushConstants(_commandBuffer, _pipeline.PipelineLayout, ConvertShaderStageFlags(stageFlags), offset, (uint)(data.Length * sizeof(T)), pData);
    }

    public override void SetPushConstants<T>(ShaderStageFlags stageFlags, uint offset, T data)
        => vkCmdPushConstants(_commandBuffer, _pipeline.PipelineLayout, ConvertShaderStageFlags(stageFlags), offset, (uint)sizeof(T), &data);

    private VkShaderStageFlags ConvertShaderStageFlags(ShaderStageFlags flags)
    {
        VkShaderStageFlags result = 0;
        if ((flags & ShaderStageFlags.Vertex) != 0) result |= VkShaderStageFlags.Vertex;
        if ((flags & ShaderStageFlags.Fragment) != 0) result |= VkShaderStageFlags.Fragment;
        return result;
    }

    public override void BindImage(BackendImage image, BackendSampler sampler, uint set, uint binding, Span<uint> dynamicOffsets)
    {
        VulkanImage vulkanImage = (VulkanImage)image;
        VulkanSampler vulkanSampler = (VulkanSampler)sampler;

        VkDescriptorSet descriptorSet = _device.DescriptorSetManager.GetOrAllocateDescriptorSet(_pipeline.PipelineLayout, set, _pipeline.DescriptorSetLayouts[set]);
        _device.DescriptorSetManager.UpdateDescriptorSet(descriptorSet, binding, vulkanImage, vulkanSampler);

        if (dynamicOffsets.Length == 0)
        {
            vkCmdBindDescriptorSets(
                commandBuffer: _commandBuffer,
                pipelineBindPoint: VkPipelineBindPoint.Graphics,
                layout: _pipeline.PipelineLayout,
                firstSet: set,
                descriptorSetCount: 1,
                descriptorSets: &descriptorSet,
                dynamicOffsetCount: 0,
                dynamicOffsets: null);
        }
        else
        {
            fixed (uint* dynamicOffsetPtr = dynamicOffsets)
            {
                vkCmdBindDescriptorSets(
                    commandBuffer: _commandBuffer,
                    pipelineBindPoint: VkPipelineBindPoint.Graphics,
                    layout: _pipeline.PipelineLayout,
                    firstSet: set,
                    descriptorSetCount: 1,
                    descriptorSets: &descriptorSet,
                    dynamicOffsetCount: (uint)dynamicOffsets.Length,
                    dynamicOffsets: dynamicOffsetPtr);
            }
        }
    }
}