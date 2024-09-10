using System;
using System.Collections.Generic;
using Engine.Vulkan;
using OpenTK;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.App;

public class MultiInstanceWindowApp : Application
{
    public override string Name => "04-MultiInstanceWindow";

    private List<GraphicsDevice> _graphicsDevices = new();
    private List<Pass> _drawPasses = new();
    private const bool _enableValidationLayers = true;
    
    private float _redValue = 0.0f;
    private float _blueValue = 0.0f;

    protected override void Initialize()
    {
        var windowManager = SdlWindowManager.GetInstance();
        RegisterWindowManager(windowManager);

        // Create two windows
        var window1 = windowManager.CreateWindow(Name + " - Window 1");
        var window2 = windowManager.CreateWindow(Name + " - Window 2");

        RenderFrame += OnRenderFrame;

        // Create a separate GraphicsDevice (and VkInstance) for each window
        _graphicsDevices.Add(new GraphicsDevice(VulkanGraphicsFactory.CreateVulkanGraphicsDevice(window1, Name + " - Instance 1", _enableValidationLayers)));
        _graphicsDevices.Add(new GraphicsDevice(VulkanGraphicsFactory.CreateVulkanGraphicsDevice(window2, Name + " - Instance 2", _enableValidationLayers)));

        CreatePasses();
    }

    private void CreatePasses()
    {
        foreach (var graphicsDevice in _graphicsDevices)
        {
            var drawPass = graphicsDevice.CreatePass(builder =>
            {
                var colorAttachmentDescription = new AttachmentDescription
                {
                    LoadOp = AttachmentLoadOp.Clear,
                    StoreOp = AttachmentStoreOp.Store,
                    ImageLayout = ImageLayout.ColorAttachmentOptimal,
                };

                builder.ConfigureColorAttachment(colorAttachmentDescription);
                builder.SetRenderTarget(graphicsDevice.GetSwapchainRenderTarget());
            });

            _drawPasses.Add(drawPass);
        }
    }

    private void OnRenderFrame(FrameEventArgs args)
    {
        _redValue = (_redValue + 0.0005f) % 1.0f;
        _blueValue = (_blueValue + 0.0003f) % 1.0f;

        for (int i = 0; i < _graphicsDevices.Count; i++)
        {
            var graphicsDevice = _graphicsDevices[i];
            var drawPass = _drawPasses[i];

            graphicsDevice.RenderFrame(frameContext =>
            {
                frameContext.UsePass(drawPass, passContext =>
                {
                    // Clear with different colors for each window
                    if (i == 0)
                        passContext.Clear(new Color3<Rgb>(_redValue, 0.0f, 0.0f));
                    else
                        passContext.Clear(new Color3<Rgb>(0.0f, 0.0f, _blueValue));
                });
            });
        }
    }
}