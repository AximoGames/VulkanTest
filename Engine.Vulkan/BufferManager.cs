using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

public unsafe class BufferManager : IDisposable
{
    private readonly VulkanDevice _device;
    private readonly VulkanCommandPool _commandPool;

    public BufferManager(VulkanDevice device, VulkanCommandPool commandPool)
    {
        _device = device;
        _commandPool = commandPool;
    }

    public VulkanBuffer CreateVertexBuffer<T>(T[] vertices) where T : unmanaged
    {
        VkBuffer buffer;
        VkDeviceMemory bufferMemory;

        var bufferSize = (uint)(Unsafe.SizeOf<T>() * vertices.Length);

        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out stagingBuffer, out stagingBufferMemory);

        fixed (void* verticesPtr = &vertices[0])
        {
            void* data;
            vkMapMemory(_device.LogicalDevice, stagingBufferMemory, 0, bufferSize, 0, &data);
            Unsafe.CopyBlock(data, verticesPtr, bufferSize);
            vkUnmapMemory(_device.LogicalDevice, stagingBufferMemory);
        }

        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferDst | VkBufferUsageFlags.VertexBuffer, VkMemoryPropertyFlags.DeviceLocal, out buffer, out bufferMemory);

        CopyBuffer(stagingBuffer, buffer, bufferSize);

        vkDestroyBuffer(_device.LogicalDevice, stagingBuffer, null);
        vkFreeMemory(_device.LogicalDevice, stagingBufferMemory, null);

        return new VulkanBuffer(typeof(T), _device, buffer, bufferMemory);
    }

    public VulkanBuffer CreateIndexBuffer(ushort[] indices)
    {
        VkBuffer buffer;
        VkDeviceMemory bufferMemory;

        uint bufferSize = (uint)(Marshal.SizeOf<ushort>() * indices.Length);

        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out stagingBuffer, out stagingBufferMemory);

        fixed (ushort* indicesPtr = &indices[0])
        {
            void* data;
            vkMapMemory(_device.LogicalDevice, stagingBufferMemory, 0, bufferSize, 0, &data);
            Unsafe.CopyBlock(data, indicesPtr, bufferSize);
            vkUnmapMemory(_device.LogicalDevice, stagingBufferMemory);
        }

        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferDst | VkBufferUsageFlags.IndexBuffer, VkMemoryPropertyFlags.DeviceLocal, out buffer, out bufferMemory);

        CopyBuffer(stagingBuffer, buffer, bufferSize);

        vkDestroyBuffer(_device.LogicalDevice, stagingBuffer, null);
        vkFreeMemory(_device.LogicalDevice, stagingBufferMemory, null);

        return new VulkanBuffer(typeof(ushort), _device, buffer, bufferMemory);
    }

    private void CreateBuffer(uint size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties, out VkBuffer buffer, out VkDeviceMemory bufferMemory)
    {
        VkBufferCreateInfo bufferInfo;
        bufferInfo.size = size;
        bufferInfo.usage = usage;
        bufferInfo.sharingMode = VkSharingMode.Exclusive;

        if (vkCreateBuffer(_device.LogicalDevice, &bufferInfo, null, out buffer) != VkResult.Success)
        {
            throw new Exception("failed to create buffer!");
        }

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

    private void CopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, ulong size)
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
            {
                return (uint)i;
            }
        }

        throw new Exception("failed to find suitable memory type!");
    }

    public void Dispose()
    {
        // vkDestroyBuffer(_device.LogicalDevice, VertexBuffer, null);
        // vkFreeMemory(_device.LogicalDevice, VertexBufferMemory, null);
        // vkDestroyBuffer(_device.LogicalDevice, IndexBuffer, null);
        // vkFreeMemory(_device.LogicalDevice, IndexBufferMemory, null);
    }
}