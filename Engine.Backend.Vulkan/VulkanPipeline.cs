using System;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using OpenTK.Mathematics;
using Vortice.ShaderCompiler;

namespace Engine.Vulkan;

internal unsafe class VulkanPipeline : BackendPipeline
{
    private readonly VulkanDevice _device;
    public VkPipeline Pipeline;
    public VkPipelineLayout PipelineLayout;
    public VkDescriptorSetLayout[] DescriptorSetLayouts;

    public VulkanPipeline(VulkanDevice device, VkPipeline pipeline, VkPipelineLayout pipelineLayout, int pipelineLayoutHash, VkDescriptorSetLayout[] descriptorSetLayouts)
        : base(device, pipelineLayoutHash)
    {
        _device = device;
        Pipeline = pipeline;
        PipelineLayout = pipelineLayout;
        DescriptorSetLayouts = descriptorSetLayouts;
    }

    public override void Dispose()
    {
        if (Pipeline != VkPipeline.Null)
        {
            vkDestroyPipeline(_device.LogicalDevice, Pipeline, null);
        }

        if (PipelineLayout != VkPipelineLayout.Null)
        {
            vkDestroyPipelineLayout(_device.LogicalDevice, PipelineLayout, null);
        }
    }
}