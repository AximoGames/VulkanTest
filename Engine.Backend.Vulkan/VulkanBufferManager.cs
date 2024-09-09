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

    public override BackendBuffer CreateBuffer<T>(BufferType bufferType, int elementCount)
    {
        uint bufferSize = (uint)(Unsafe.SizeOf<T>() * elementCount);
        VkBuffer buffer;
        VkDeviceMemory bufferMemory;

        VkBufferUsageFlags vkBufferType = bufferType switch
        {
            BufferType.Vertex => VkBufferUsageFlags.VertexBuffer,
            BufferType.Index => VkBufferUsageFlags.IndexBuffer,
            _ => throw new InvalidOperationException(),
        };

        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferDst | vkBufferType, VkMemoryPropertyFlags.DeviceLocal, out buffer, out bufferMemory);

        return new VulkanBuffer(typeof(T), bufferSize, _device, buffer, bufferMemory);
    }

    public override BackendBuffer CreateUniformBuffer<T>()
    {
        uint bufferSize = (uint)(Unsafe.SizeOf<T>());
        VkBuffer buffer;
        VkDeviceMemory bufferMemory;

        CreateBuffer(bufferSize, VkBufferUsageFlags.UniformBuffer, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out buffer, out bufferMemory);

        return new VulkanBuffer(typeof(T), bufferSize, _device, buffer, bufferMemory);
    }

    public override void UpdateUniformBuffer<T>(BackendBuffer buffer, T data)
    {
        var vulkanBuffer = (VulkanBuffer)buffer;
        uint bufferSize = (uint)Unsafe.SizeOf<T>();

        void* mappedMemory;
        vkMapMemory(_device.LogicalDevice, vulkanBuffer.Memory, 0, bufferSize, 0, &mappedMemory);
        Unsafe.Copy(mappedMemory, ref data);
        vkUnmapMemory(_device.LogicalDevice, vulkanBuffer.Memory);
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
        CopyBuffer(stagingBuffer, ((VulkanBuffer)destinationBuffer).Buffer, bufferSize, 0, destinationOffset);

        vkDestroyBuffer(_device.LogicalDevice, stagingBuffer, null);
        vkFreeMemory(_device.LogicalDevice, stagingBufferMemory, null);
    }

    internal void CreateBuffer(uint size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties, out VkBuffer buffer, out VkDeviceMemory bufferMemory)
    {
        VkBufferCreateInfo bufferInfo = new()
        {
            size = size,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive
        };

        if (vkCreateBuffer(_device.LogicalDevice, &bufferInfo, null, out buffer) != VkResult.Success)
            throw new Exception("failed to create buffer!");

        VkMemoryRequirements memRequirements;
        vkGetBufferMemoryRequirements(_device.LogicalDevice, buffer, out memRequirements);

        VkMemoryAllocateInfo allocInfo = new()
        {
            allocationSize = memRequirements.size,
            memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, properties),
        };

        if (vkAllocateMemory(_device.LogicalDevice, &allocInfo, null, out bufferMemory) != VkResult.Success)
            throw new Exception("failed to allocate buffer memory!");

        vkBindBufferMemory(_device.LogicalDevice, buffer, bufferMemory, 0);
    }

    private void CopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, ulong size, ulong srcOffset, ulong dstOffset)
    {
        VkCommandBufferAllocateInfo allocInfo = new()
        {
            level = VkCommandBufferLevel.Primary,
            commandPool = _commandPool.Handle,
            commandBufferCount = 1,
        };

        VkCommandBuffer commandBuffer;
        vkAllocateCommandBuffers(_device.LogicalDevice, &allocInfo, &commandBuffer);

        VkCommandBufferBeginInfo beginInfo = new()
        {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit
        };

        vkBeginCommandBuffer(commandBuffer, &beginInfo);

        VkBufferCopy copyRegion = new()
        {
            srcOffset = srcOffset,
            dstOffset = dstOffset,
            size = size
        };
        vkCmdCopyBuffer(commandBuffer, srcBuffer, dstBuffer, 1, &copyRegion);

        vkEndCommandBuffer(commandBuffer);

        VkSubmitInfo submitInfo = new()
        {
            commandBufferCount = 1,
            pCommandBuffers = &commandBuffer
        };

        vkQueueSubmit(_device.GraphicsQueue, 1, &submitInfo, VkFence.Null);
        vkQueueWaitIdle(_device.GraphicsQueue);

        vkFreeCommandBuffers(_device.LogicalDevice, _commandPool.Handle, 1, &commandBuffer);
    }

    public uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
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