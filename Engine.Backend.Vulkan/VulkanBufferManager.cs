using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanBufferManager : BackendBufferManager
{
    private readonly VulkanDevice _device;
    private readonly VulkanCommandPool _commandPool;

    public VulkanBufferManager(VulkanDevice device, VulkanCommandPool commandPool)
    {
        _device = device;
        _commandPool = commandPool;
    }

    public override BackendBuffer CreateBuffer<T>(BufferType bufferType, int vertexCount)
    {
        uint bufferSize = (uint)(Unsafe.SizeOf<T>() * vertexCount);
        VkBuffer buffer;
        VkDeviceMemory bufferMemory;

        VkBufferUsageFlags vkBufferType = bufferType switch
        {
            BufferType.Vertex => VkBufferUsageFlags.VertexBuffer,
            BufferType.Index => VkBufferUsageFlags.IndexBuffer,
            _ => throw new InvalidOperationException(),
        };

        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferDst | vkBufferType, VkMemoryPropertyFlags.DeviceLocal, out buffer, out bufferMemory);

        return new VulkanBackendBuffer(typeof(T), _device, buffer, bufferMemory);
    }

    public override void CopyBuffer<T>(T[] source, int sourceStartIndex, BackendBuffer destinationBuffer, int destinationStartIndex, int count)
    {
        var elementSize = Unsafe.SizeOf<T>();
        uint bufferSize = (uint)(elementSize * count);
        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out stagingBuffer, out stagingBufferMemory);

        fixed (void* verticesPtr = &source[sourceStartIndex])
        {
            void* data;
            vkMapMemory(_device.LogicalDevice, stagingBufferMemory, 0, bufferSize, 0, &data);
            Unsafe.CopyBlock(data, verticesPtr, bufferSize);
            vkUnmapMemory(_device.LogicalDevice, stagingBufferMemory);
        }

        uint destinationOffset = (uint)(elementSize * destinationStartIndex);
        CopyBuffer(stagingBuffer, ((VulkanBackendBuffer)destinationBuffer).Buffer, bufferSize, 0, destinationOffset);

        vkDestroyBuffer(_device.LogicalDevice, stagingBuffer, null);
        vkFreeMemory(_device.LogicalDevice, stagingBufferMemory, null);
    }

    private void CreateBuffer(uint size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties, out VkBuffer buffer, out VkDeviceMemory bufferMemory)
    {
        VkBufferCreateInfo bufferInfo;
        bufferInfo.size = size;
        bufferInfo.usage = usage;
        bufferInfo.sharingMode = VkSharingMode.Exclusive;

        if (vkCreateBuffer(_device.LogicalDevice, &bufferInfo, null, out buffer) != VkResult.Success)
            throw new Exception("failed to create buffer!");

        VkMemoryRequirements memRequirements;
        vkGetBufferMemoryRequirements(_device.LogicalDevice, buffer, out memRequirements);

        VkMemoryAllocateInfo allocInfo;
        allocInfo.allocationSize = memRequirements.size;
        allocInfo.memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, properties);

        if (vkAllocateMemory(_device.LogicalDevice, &allocInfo, null, out bufferMemory) != VkResult.Success)
        {
            throw new Exception("failed to allocate buffer memory!");
        }

        vkBindBufferMemory(_device.LogicalDevice, buffer, bufferMemory, 0);
    }

    private void CopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, ulong size, ulong srcOffset, ulong dstOffset)
    {
        VkCommandBufferAllocateInfo allocInfo;
        allocInfo.level = VkCommandBufferLevel.Primary;
        allocInfo.commandPool = _commandPool.Handle;
        allocInfo.commandBufferCount = 1;

        VkCommandBuffer commandBuffer;
        vkAllocateCommandBuffers(_device.LogicalDevice, &allocInfo, &commandBuffer);

        VkCommandBufferBeginInfo beginInfo;
        beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;

        vkBeginCommandBuffer(commandBuffer, &beginInfo);

        VkBufferCopy copyRegion;
        copyRegion.srcOffset = srcOffset;
        copyRegion.dstOffset = dstOffset;
        copyRegion.size = size;
        vkCmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);

        vkEndCommandBuffer(commandBuffer);

        VkSubmitInfo submitInfo;
        submitInfo.commandBufferCount = 1;
        submitInfo.pCommandBuffers = &commandBuffer;

        vkQueueSubmit(_device.GraphicsQueue, 1, &submitInfo, VkFence.Null);
        vkQueueWaitIdle(_device.GraphicsQueue);

        vkFreeCommandBuffers(_device.LogicalDevice, _commandPool.Handle, 1, &commandBuffer);
    }

    private uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
    {
        VkPhysicalDeviceMemoryProperties memProperties;
        vkGetPhysicalDeviceMemoryProperties(_device.PhysicalDevice, out memProperties);

        for (int i = 0; i < memProperties.memoryTypeCount; i++)
        {
            if ((typeFilter & (1 << i)) != 0 && (memProperties.memoryTypes[i].propertyFlags & properties) == properties)
                return (uint)i;
        }

        throw new Exception("failed to find suitable memory type!");
    }

    public override void Dispose()
    {
    }
}