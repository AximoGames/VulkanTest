using System;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanDescriptorSetManager
{
    private readonly VulkanDevice _device;
    private readonly VkDescriptorPool _descriptorPool;
    private readonly uint _maxSets;
    private uint _allocatedSets;

    private Dictionary<(int, uint, uint, VkBuffer), VkDescriptorSet> _descriptorSetCache = new();

    public VulkanDescriptorSetManager(VulkanDevice device, uint maxSets)
    {
        _device = device;
        _maxSets = maxSets;
        _descriptorPool = CreateDescriptorPool(maxSets);
    }

    public VkDescriptorSet GetOrCreateDescriptorSet(VulkanPipeline pipeline, uint set, uint binding, VulkanBuffer buffer)
    {
           var key = (pipeline.PipelineLayoutHash, set, binding, buffer.Buffer);

    if (_descriptorSetCache.TryGetValue(key, out var descriptorSet))
        return descriptorSet;

    descriptorSet = AllocateDescriptorSet(pipeline.DescriptorSetLayouts[set]);
    UpdateDescriptorSet(descriptorSet, binding, buffer);

    _descriptorSetCache[key] = descriptorSet;
    return descriptorSet;
    }


private VkDescriptorSet AllocateDescriptorSet(VkDescriptorSetLayout layout)
{
    VkDescriptorSetAllocateInfo allocInfo = new VkDescriptorSetAllocateInfo
    {
        descriptorPool = _descriptorPool,
        descriptorSetCount = 1,
        pSetLayouts = &layout
    };

    VkDescriptorSet descriptorSet;
    vkAllocateDescriptorSets(_device.LogicalDevice, &allocInfo, &descriptorSet).CheckResult();
    return descriptorSet;
}

    private void UpdateDescriptorSet(VkDescriptorSet descriptorSet, uint binding, VulkanBuffer buffer)
    {
        VkDescriptorBufferInfo bufferInfo = new VkDescriptorBufferInfo
        {
            buffer = buffer.Buffer,
            offset = 0,
            range = buffer.Size
        };

        VkWriteDescriptorSet descriptorWrite = new VkWriteDescriptorSet
        {
            dstSet = descriptorSet,
            dstBinding = binding,
            dstArrayElement = 0,
            descriptorType = VkDescriptorType.UniformBuffer,
            descriptorCount = 1,
            pBufferInfo = &bufferInfo
        };

        vkUpdateDescriptorSets(_device.LogicalDevice, 1, &descriptorWrite, 0, null);
    }

    private VkDescriptorPool CreateDescriptorPool(uint maxSets)
    {
        VkDescriptorPoolSize poolSize = new VkDescriptorPoolSize
        {
            type = VkDescriptorType.UniformBuffer,
            descriptorCount = maxSets
        };

        VkDescriptorPoolCreateInfo poolInfo = new VkDescriptorPoolCreateInfo
        {
            poolSizeCount = 1,
            pPoolSizes = &poolSize,
            maxSets = maxSets
        };

        VkDescriptorPool descriptorPool;
        vkCreateDescriptorPool(_device.LogicalDevice, &poolInfo, null, out descriptorPool).CheckResult();
        return descriptorPool;
    }

    public void Dispose()
    {
        vkDestroyDescriptorPool(_device.LogicalDevice, _descriptorPool, null);
    }
}