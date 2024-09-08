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

    public VulkanPipeline(VulkanDevice device, VkPipeline pipeline, VkPipelineLayout pipelineLayout)
        : base(device)
    {
        _device = device;
        Pipeline = pipeline;
        PipelineLayout = pipelineLayout;
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