using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Vortice;

public unsafe class BufferManager : IDisposable
{
    private readonly GraphicsDevice _device;

    public VkBuffer VertexBuffer;
    public VkDeviceMemory VertexBufferMemory;
    public VkBuffer IndexBuffer;
    public VkDeviceMemory IndexBufferMemory;

    public BufferManager(GraphicsDevice device)
    {
        _device = device;
    }

    public void CreateVertexBuffer<T>(T[] vertices) where T : unmanaged
    {
        var bufferSize = (uint)(Unsafe.SizeOf<T>() * vertices.Length);

        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out stagingBuffer, out stagingBufferMemory);

        fixed (void* verticesPtr = &vertices[0])
        {
            void* data;
            vkMapMemory(_device.VkDevice, stagingBufferMemory, 0, bufferSize, 0, &data);
            Unsafe.CopyBlock(data, verticesPtr, bufferSize);
            vkUnmapMemory(_device.VkDevice, stagingBufferMemory);
        }

        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferDst | VkBufferUsageFlags.VertexBuffer, VkMemoryPropertyFlags.DeviceLocal, out VertexBuffer, out VertexBufferMemory);

        CopyBuffer(stagingBuffer, VertexBuffer, bufferSize);

        vkDestroyBuffer(_device.VkDevice, stagingBuffer, null);
        vkFreeMemory(_device.VkDevice, stagingBufferMemory, null);
    }

    public void CreateIndexBuffer(ushort[] indices)
    {
        uint bufferSize = (uint)(Marshal.SizeOf<ushort>() * indices.Length);

        VkBuffer stagingBuffer;
        VkDeviceMemory stagingBufferMemory;
        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferSrc, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, out stagingBuffer, out stagingBufferMemory);

        fixed (ushort* indiciesPtr = &indices[0])
        {
            void* data;
            vkMapMemory(_device.VkDevice, stagingBufferMemory, 0, bufferSize, 0, &data);
            Unsafe.CopyBlock(data, indiciesPtr, bufferSize);
            vkUnmapMemory(_device.VkDevice, stagingBufferMemory);
        }

        CreateBuffer(bufferSize, VkBufferUsageFlags.TransferDst | VkBufferUsageFlags.IndexBuffer, VkMemoryPropertyFlags.DeviceLocal, out IndexBuffer, out IndexBufferMemory);

        CopyBuffer(stagingBuffer, IndexBuffer, bufferSize);

        vkDestroyBuffer(_device.VkDevice, stagingBuffer, null);
        vkFreeMemory(_device.VkDevice, stagingBufferMemory, null);
    }

    private void CreateBuffer(uint size, VkBufferUsageFlags usage, VkMemoryPropertyFlags properties, out VkBuffer buffer, out VkDeviceMemory bufferMemory)
    {
        VkBufferCreateInfo bufferInfo;
        bufferInfo.size = size;
        bufferInfo.usage = usage;
        bufferInfo.sharingMode = VkSharingMode.Exclusive;

        if (vkCreateBuffer(_device.VkDevice, &bufferInfo, null, out buffer) != VkResult.Success)
        {
            throw new Exception("failed to create buffer!");
        }

        VkMemoryRequirements memRequirements;
        vkGetBufferMemoryRequirements(_device.VkDevice, buffer, out memRequirements);

        VkMemoryAllocateInfo allocInfo;
        allocInfo.allocationSize = memRequirements.size;
        allocInfo.memoryTypeIndex = FindMemoryType(memRequirements.memoryTypeBits, properties);

        if (vkAllocateMemory(_device.VkDevice, &allocInfo, null, out bufferMemory) != VkResult.Success)
        {
            throw new Exception("failed to allocate buffer memory!");
        }

        vkBindBufferMemory(_device.VkDevice, buffer, bufferMemory, 0);
    }

    private void CopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, ulong size)
    {
        VkCommandBufferAllocateInfo allocInfo;
        allocInfo.level = VkCommandBufferLevel.Primary;
        allocInfo.commandPool = _device.CommandPool;
        allocInfo.commandBufferCount = 1;

        VkCommandBuffer commandBuffer;
        vkAllocateCommandBuffers(_device.VkDevice, &allocInfo, &commandBuffer);

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

        vkFreeCommandBuffers(_device.VkDevice, _device.CommandPool, 1, &commandBuffer);
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
        vkDestroyBuffer(_device.VkDevice, VertexBuffer, null);
        vkFreeMemory(_device.VkDevice, VertexBufferMemory, null);
        vkDestroyBuffer(_device.VkDevice, IndexBuffer, null);
        vkFreeMemory(_device.VkDevice, IndexBufferMemory, null);
    }
}