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

    private void OnDraw(VkCommandBuffer commandBuffer, VkFramebuffer framebuffer, VkExtent2D size, VkPipeline pipeline)
    {
        float g = _green + 0.01f;
        if (g > 1.0f)
            g = 0.0f;
        _green = g;

        VkClearValue clearValue = new VkClearValue(1.0f, _green, 0.0f, 1.0f);

        // Begin the render pass.
        VkRenderPassBeginInfo renderPassBeginInfo = new VkRenderPassBeginInfo
        {
            renderPass = _graphicsDevice.RenderPass,
            framebuffer = framebuffer,
            renderArea = new VkRect2D(size),
            clearValueCount = 1,
            pClearValues = &clearValue
        };
        vkCmdBeginRenderPass(commandBuffer, &renderPassBeginInfo, VkSubpassContents.Inline);
        vkCmdBindPipeline(commandBuffer, VkPipelineBindPoint.Graphics, pipeline);
        //vkCmdDraw(commandBuffer, 3, 1, 0, 0);

        vkCmdBindVertexBuffer(commandBuffer, 0, _graphicsDevice.VertexBuffer);
        vkCmdBindIndexBuffer(commandBuffer, _graphicsDevice.IndexBuffer, 0, VkIndexType.Uint16);

        //vkCmdDraw(commandBuffer, (uint)_graphicsDevice.Vertices.Length, 1, 0, 0);
        vkCmdDrawIndexed(commandBuffer, _graphicsDevice.Indices.Length, 1, 0, 0, 0);

        vkCmdSetBlendConstants(commandBuffer, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
        vkCmdEndRenderPass(commandBuffer);
    }
}