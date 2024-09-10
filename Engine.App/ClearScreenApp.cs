using System;
using System.Collections.Generic;
using Engine.Vulkan;
using OpenTK;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.App;

public class ClearScreenApp : Application
{
    public override string Name => "02-ClearScreen";

    private GraphicsDevice _graphicsDevice;
    private const bool _enableValidationLayers = true;
    
    private Pass _drawPass;
    private float _greenValue = 0.0f;

    protected override void Initialize()
    {
        var windowManager = SdlWindowManager.GetInstance();
        RegisterWindowManager(windowManager);
        var window = windowManager.CreateWindow(Name);
        RenderFrame += OnRenderFrame;

        _graphicsDevice = new GraphicsDevice(VulkanGraphicsFactory.CreateVulkanGraphicsDevice(window, Name, _enableValidationLayers));
        CreatePasses();
    }

    private void CreatePasses()
    {
        _drawPass = _graphicsDevice.CreatePass(builder =>
        {
            var colorAttachmentDescription = new AttachmentDescription
            {
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                ImageLayout = ImageLayout.ColorAttachmentOptimal,
            };

            builder.ConfigureColorAttachment(colorAttachmentDescription);
            builder.SetRenderTarget(_graphicsDevice.GetSwapchainRenderTarget());
        });
    }

    private void OnRenderFrame(FrameEventArgs args)
    {
        _greenValue = (_greenValue + 0.0003f) % 1.0f;

        _graphicsDevice.RenderFrame(frameContext =>
        {
            frameContext.UsePass(_drawPass, passContext =>
            {
                // Clear
                passContext.Clear(new Color3<Rgb>(0.0f, _greenValue, 0.0f));
            });
        });
    }
}