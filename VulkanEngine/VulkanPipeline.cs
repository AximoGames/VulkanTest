using System;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using OpenTK.Mathematics;
using Vortice.ShaderCompiler;

namespace VulkanTest;

public unsafe class VulkanPipeline : IDisposable
{
    private readonly VulkanDevice _device;
    public VkPipeline PipelineHandle;
    public VkPipelineLayout PipelineLayoutHandle;

    public VulkanPipeline(VulkanDevice device , VkPipeline pipelineHandle, VkPipelineLayout pipelineLayoutHandle)
    {
        _device = device;
        PipelineHandle = pipelineHandle;
        PipelineLayoutHandle = pipelineLayoutHandle;
    }

    public void Dispose()
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