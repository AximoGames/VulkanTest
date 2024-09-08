using OpenTK.Mathematics;
using Vortice.Vulkan;

namespace Engine.Vulkan;

internal abstract class VulkanRenderTarget : IDisposable
{
    protected readonly VulkanDevice Device;
    public abstract Vector2i Extent { get; }
    public abstract uint ImageCount { get; }

    public abstract VulkanImage GetImage(uint index);

    protected VulkanRenderTarget(VulkanDevice device)
    {
        Device = device;
    }

    public abstract void Dispose();
}