using System;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using OpenTK.Mathematics;
using Vortice.ShaderCompiler;

namespace Engine.Vulkan;

internal unsafe class VulkanPipeline : BackendPipeline
{
    private readonly VulkanDevice _device;
    public VkPipeline PipelineHandle;
    public VkPipelineLayout PipelineLayoutHandle;

    public VulkanPipeline(VulkanDevice device, VkPipeline pipelineHandle, VkPipelineLayout pipelineLayoutHandle)
        : base(device)
    {
        _device = device;
        PipelineHandle = pipelineHandle;
        PipelineLayoutHandle = pipelineLayoutHandle;
    }

    public override void Dispose()
    {
        if (PipelineHandle != VkPipeline.Null)
        {
            vkDestroyPipeline(_device.LogicalDevice, PipelineHandle, null);
        }

        if (PipelineLayoutHandle != VkPipelineLayout.Null)
        {
            vkDestroyPipelineLayout(_device.LogicalDevice, PipelineLayoutHandle, null);
        }
    }
}