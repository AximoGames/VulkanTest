﻿using System;
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

        VkClearValue clearValue = new VkClearValue(1.0f, _greenValue, 0.0f, 1.0f);

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

        vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, _graphicsDevice.Pipeline.PipelineHandle);

        var renderContext = new RenderContext(_graphicsDevice.VulkanDevice, _graphicsDevice.BufferManager, commandBuffer, size);
        renderContext.Draw();

        vkCmdEndRendering(commandBuffer);
    }
}