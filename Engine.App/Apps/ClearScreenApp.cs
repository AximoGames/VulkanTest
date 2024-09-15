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

    private Device _graphicsDevice;

    private Pass _drawPass;
    private float _greenValue = 0.0f;

    protected override void Initialize()
    {
        SdlWindowManager windowManager = SdlWindowManager.GetInstance();
        RegisterWindowManager(windowManager);
        Window window = windowManager.CreateWindow(Name);
        RenderFrame += OnRenderFrame;

        _graphicsDevice = new VulkanFactory()
            .CreateInstance(windowManager, Name)
            .CreateDevice(window);

        CreatePasses();
    }

    private void CreatePasses()
    {
        _drawPass = _graphicsDevice.CreatePass(builder =>
        {
            AttachmentDescription colorAttachmentDescription = new AttachmentDescription
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