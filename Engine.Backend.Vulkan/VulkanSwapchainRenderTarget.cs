using OpenTK.Mathematics;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanSwapchainRenderTarget : VulkanRenderTarget
{
    private readonly VulkanSwapchain _swapchain;
    private VulkanImage[] _images;

    public VulkanSwapchainRenderTarget(VulkanDevice device, VulkanSwapchain swapchain, VulkanImage[] images)
        : base(device)
    {
        _swapchain = swapchain;
        _images = images;
    }

    public override Vector2i Extent { get; }
    public override uint ImageCount { get; }

    public override VulkanImage GetImage(uint index)
        => _images[index];

    public override void Dispose()
    {
        for (int i = 0; i < _images.Length; i++)
        {
            // Only destroy the image view, not the image itself (as it's owned by the swapchain)
            vkDestroyImageView(Device.LogicalDevice, _images[i].ImageView, null);
            _images[i].Dispose();
        }
    }
}