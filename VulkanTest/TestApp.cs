using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Vortice;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace DrawTriangle;

public unsafe class TestApp : Application
{
#if DEBUG
    private static bool EnableValidationLayers = true;
#else
		private static bool EnableValidationLayers = false;
#endif

    [NotNull]
    private GraphicsDevice _graphicsDevice = default!;

    private float _green = 0.0f;

    public override string Name => "01-ClearScreen";

    protected override void Initialize()
    {
        base.Initialize();
        // Need to initialize
        vkInitialize().CheckResult();

        _graphicsDevice = new GraphicsDevice(Name, EnableValidationLayers, MainWindow);
    }

    protected override void OnRenderFrame()
    {
        _graphicsDevice.RenderFrame(OnDraw);
    }

    private void OnDraw(VkCommandBuffer commandBuffer, VkExtent2D size)
    {
        float g = _green + 0.01f;
        if (g > 1.0f)
            g = 0.0f;
        _green = g;

        VkClearValue clearValue = new VkClearValue(1.0f, _green, 0.0f, 1.0f);

        VkRenderingAttachmentInfo colorAttachmentInfo = new VkRenderingAttachmentInfo
        {
            imageView = _graphicsDevice.Swapchain.GetImageView(_graphicsDevice.CurrentSwapchainImageIndex),
            imageLayout = VkImageLayout.ColorAttachmentOptimal,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            clearValue = clearValue
        };

        VkRenderingInfo renderingInfo = new VkRenderingInfo
        {
            renderArea = new VkRect2D(VkOffset2D.Zero, size),
            layerCount = 1,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachmentInfo
        };

        vkCmdBeginRendering(commandBuffer, &renderingInfo);

        vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, _graphicsDevice.Pipeline.Handle);

        vkCmdBindVertexBuffer(commandBuffer, 0, _graphicsDevice.BufferManager.VertexBuffer);
        vkCmdBindIndexBuffer(commandBuffer, _graphicsDevice.BufferManager.IndexBuffer, 0, VkIndexType.Uint16);

        vkCmdDrawIndexed(commandBuffer, (uint)_graphicsDevice.Indices.Length, 1, 0, 0, 0);

        vkCmdSetBlendConstants(commandBuffer, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

        vkCmdEndRendering(commandBuffer);
    }
}