using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Vortice;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VulkanTest;

public unsafe class TestApp : Application
{
#if DEBUG
    private static bool EnableValidationLayers = true;
#else
		private static bool EnableValidationLayers = false;
#endif

    [NotNull]
    private GraphicsDevice _graphicsDevice = default!;

    private float _greenValue = 0.0f;

    public override string Name => "01-DrawTriangle";

    protected override void Initialize()
    {
        base.Initialize();
        // Need to initialize
        vkInitialize().CheckResult();

        _graphicsDevice = new GraphicsDevice(Name, EnableValidationLayers, MainWindow);
    }

    protected override void OnRenderFrame()
    {
        try
        {
            _graphicsDevice.RenderFrame(OnDraw);
        }
        catch (VkException ex)
        {
            Log.Error($"Vulkan error occurred: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected error occurred: {ex.Message}");
        }
    }

    private void OnDraw(VkCommandBuffer commandBuffer, VkExtent2D size)
    {
        float g = _greenValue + 0.0003f;
        if (g > 1.0f)
            g = 0.0f;
        _greenValue = g;

        var renderContext = new RenderContext(_graphicsDevice.VulkanDevice, _graphicsDevice.BufferManager, commandBuffer, size);
        renderContext.Clear(new VkClearColorValue(0, 0, _greenValue));
        renderContext.BindVertexBuffer();
        renderContext.BindIndexBuffer();
        renderContext.DrawIndexed(6);
    }
}