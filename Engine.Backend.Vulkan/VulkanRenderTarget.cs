using OpenTK.Mathematics;
using Vortice.Vulkan;

namespace Engine.Vulkan;

internal abstract class VulkanRenderTarget : BackendRenderTarget
{
    protected readonly VulkanDevice Device;
    public abstract uint ImageCount { get; }

    protected VulkanRenderTarget(VulkanDevice device)
    {
        Device = device;
    }
}