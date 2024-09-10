using OpenTK.Mathematics;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

internal unsafe class VulkanImageRenderTarget : VulkanRenderTarget
{
    private VulkanImage[] _images;

    public VulkanImageRenderTarget(VulkanDevice device, VulkanImage[] images)
        : base(device)
    {
        _images = images;
        ImageCount = (uint)images.Length;
        Extent = images[0].Extent;
    }

    public override Vector2i Extent { get; }
    public override uint ImageCount { get; }

    public override BackendImage GetImage(uint index)
        => _images[index];

    public override void Dispose()
    {
        for (int i = 0; i < _images.Length; i++) 
            _images[i].Dispose();
    }
}
