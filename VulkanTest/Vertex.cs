using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using Vortice.Vulkan;

namespace Vortice;

public struct Vertex
{
    public Vector2 pos;
    public Vector3 color;

    public static VkVertexInputBindingDescription getBindingDescription()
    {
        VkVertexInputBindingDescription bindingDescription;
        bindingDescription.binding = 0;
        bindingDescription.stride = (uint)Marshal.SizeOf<Vertex>();
        bindingDescription.inputRate = VkVertexInputRate.Vertex;
        return bindingDescription;
    }

    public static VkVertexInputAttributeDescription[] getAttributeDescriptions()
    {
        var attributeDescriptions = new VkVertexInputAttributeDescription[2];

        attributeDescriptions[0].binding = 0;
        attributeDescriptions[0].location = 0;
        attributeDescriptions[0].format = VkFormat.R32G32Sfloat;
        attributeDescriptions[0].offset = 0;

        attributeDescriptions[1].binding = 0;
        attributeDescriptions[1].location = 1;
        attributeDescriptions[1].format = VkFormat.R32G32B32Sfloat;
        attributeDescriptions[1].offset = (uint)Marshal.SizeOf<Vector2>();

        return attributeDescriptions;
    }
}