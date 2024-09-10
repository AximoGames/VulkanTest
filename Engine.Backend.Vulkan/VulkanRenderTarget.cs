using OpenTK.Mathematics;
using Vortice.Vulkan;

namespace Engine.Vulkan;

internal abstract class VulkanRenderTarget : BackendRenderTarget
{
    protected readonly VulkanDevice Device;

    protected VulkanRenderTarget(VulkanDevice device)
    {
        Device = device;
    }
}