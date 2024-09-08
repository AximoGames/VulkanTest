using OpenTK.Mathematics;
using Vortice.Vulkan;

namespace Engine.Vulkan;

internal abstract class VulkanRenderTarget : BackendRenderTarget
{
    protected readonly VulkanDevice Device;
    public abstract uint ImageCount { get; }

    public abstract VulkanImage GetImage(uint index);

    protected VulkanRenderTarget(VulkanDevice device)
    {
        Device = device;
    }
}