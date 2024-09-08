using Vortice.Vulkan;

namespace Engine.Vulkan;

internal abstract class VulkanRenderTarget : IDisposable
{
    protected readonly VulkanDevice Device;
    public uint Width { get; protected set; }
    public uint Height { get; protected set; }
    public uint ImageCount { get; protected set; }

    public abstract BackendImage GetImage(uint index);
    
    protected VulkanRenderTarget(VulkanDevice device, uint width, uint height, uint imageCount)
    {
        Device = device;
        Width = width;
        Height = height;
        ImageCount = imageCount;
    }

    public abstract void Dispose();
}