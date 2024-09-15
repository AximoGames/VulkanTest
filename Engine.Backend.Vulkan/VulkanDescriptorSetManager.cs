using System;
using System.Collections.Generic;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanDescriptorSetManager
{
    private readonly VulkanDevice _device;
    private readonly VkDescriptorPool _descriptorPool;
    private readonly uint _maxSets;
    private uint _allocatedSets;
    private Dictionary<(VkPipelineLayout layout, uint set), VkDescriptorSet> _descriptorSets;

    public VulkanDescriptorSetManager(VulkanDevice device, uint maxSets)
    {
        _device = device;
        _maxSets = maxSets;
        _descriptorPool = CreateDescriptorPool(maxSets);
        _descriptorSets = new Dictionary<(VkPipelineLayout, uint), VkDescriptorSet>();
    }

    public VkDescriptorSet GetOrAllocateDescriptorSet(VkPipelineLayout pipelineLayout, uint set, VkDescriptorSetLayout layout)
    {
        (VkPipelineLayout pipelineLayout, uint set) key = (pipelineLayout, set);
        if (!_descriptorSets.TryGetValue(key, out VkDescriptorSet descriptorSet))
        {
            descriptorSet = AllocateDescriptorSet(layout);
            _descriptorSets[key] = descriptorSet;
        }
        return descriptorSet;
    }

    private VkDescriptorSet AllocateDescriptorSet(VkDescriptorSetLayout layout)
    {
        uint* pDescriptorCounts = stackalloc uint[] { 1 };
        VkDescriptorSetVariableDescriptorCountAllocateInfo variableCountInfo = new VkDescriptorSetVariableDescriptorCountAllocateInfo
        {
            descriptorSetCount = 1,
            pDescriptorCounts = pDescriptorCounts
        };

        VkDescriptorSetAllocateInfo allocInfo = new VkDescriptorSetAllocateInfo
        {
            pNext = &variableCountInfo,
            descriptorPool = _descriptorPool,
            descriptorSetCount = 1,
            pSetLayouts = &layout
        };

        VkDescriptorSet descriptorSet;
        vkAllocateDescriptorSets(_device.LogicalDevice, &allocInfo, &descriptorSet).CheckResult();
        return descriptorSet;
    }

    public void UpdateDescriptorSet(VkDescriptorSet descriptorSet, uint binding, VulkanBuffer buffer)
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
            descriptorType = VkDescriptorType.UniformBufferDynamic,
            descriptorCount = 1,
            pBufferInfo = &bufferInfo
        };

        vkUpdateDescriptorSets(_device.LogicalDevice, 1, &descriptorWrite, 0, null);
    }

    public void UpdateDescriptorSet(VkDescriptorSet descriptorSet, uint binding, VulkanImage image, VulkanSampler sampler)
    {
        VkDescriptorImageInfo imageInfo = new VkDescriptorImageInfo
        {
            imageLayout = VkImageLayout.ShaderReadOnlyOptimal,
            imageView = image.ImageView,
            sampler = sampler.Sampler
        };

        VkWriteDescriptorSet descriptorWrite = new VkWriteDescriptorSet
        {
            dstSet = descriptorSet,
            dstBinding = binding,
            dstArrayElement = 0,
            descriptorType = VkDescriptorType.CombinedImageSampler,
            descriptorCount = 1,
            pImageInfo = &imageInfo
        };

        vkUpdateDescriptorSets(_device.LogicalDevice, 1, &descriptorWrite, 0, null);
    }

    public void FreeDescriptorSet(VkPipelineLayout pipelineLayout, uint set)
    {
        (VkPipelineLayout pipelineLayout, uint set) key = (pipelineLayout, set);
        if (_descriptorSets.TryGetValue(key, out VkDescriptorSet descriptorSet))
        {
            vkFreeDescriptorSets(_device.LogicalDevice, _descriptorPool, 1, &descriptorSet);
            _descriptorSets.Remove(key);
        }
    }

    public void Cleanup()
    {
        foreach (VkDescriptorSet descriptorSet in _descriptorSets.Values)
        {
            vkFreeDescriptorSets(_device.LogicalDevice, _descriptorPool, 1, &descriptorSet);
        }
        _descriptorSets.Clear();
        vkDestroyDescriptorPool(_device.LogicalDevice, _descriptorPool, null);
    }

    private VkDescriptorPool CreateDescriptorPool(uint maxSets)
    {
        VkDescriptorPoolSize poolSize = new VkDescriptorPoolSize
        {
            type = VkDescriptorType.UniformBufferDynamic,
            descriptorCount = maxSets
        };

        VkDescriptorPoolCreateInfo poolInfo = new VkDescriptorPoolCreateInfo
        {
            flags = VkDescriptorPoolCreateFlags.UpdateAfterBind,
            poolSizeCount = 1,
            pPoolSizes = &poolSize,
            maxSets = maxSets
        };

        VkDescriptorPool descriptorPool;
        vkCreateDescriptorPool(_device.LogicalDevice, &poolInfo, null, out descriptorPool).CheckResult();
        return descriptorPool;
    }
}