using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanImageManager : BackendImageManager
{
    private readonly VulkanDevice _device;
    private readonly VulkanBufferManager _bufferManager;

    public VulkanImageManager(VulkanDevice device, VulkanBufferManager bufferManager)
    {
        _device = device;
        _bufferManager = bufferManager;
    }

    public override BackendImage CreateRenderTargetImage(uint width, uint height)
    {
        throw new NotImplementedException();

    }
}
