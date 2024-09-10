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
    private GraphicsDevice _graphicsDevice;
    private float _greenValue = 0.0f;
    private bool EnableValidationLayers = true;

    private string[] suppressDebugMessages =
    [
        //"VUID-VkPresentInfoKHR-pImageIndices-01430",
    ];

    private Pass _drawPass;

    public override string Name => "02-ClearScreen";

    protected override void Initialize()
    {
        var windowManager = SdlWindowManager.GetInstance();
        RegisterWindowManager(windowManager);
        var window = windowManager.CreateWindow(Name);
        RenderFrame += (e) => { OnRenderFrame(); };

        _graphicsDevice = new GraphicsDevice(VulkanGraphicsFactory.CreateVulkanGraphicsDevice(Name, EnableValidationLayers, window, suppressDebugMessages));
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

    protected override void OnRenderFrame()
    {
        float g = _greenValue + 0.0003f;
        if (g > 1.0f)
            g = 0.0f;
        _greenValue = g;

        _graphicsDevice.RenderFrame(frameContext =>
        {
            frameContext.UsePass(_drawPass, passContext =>
            {
                passContext.Clear(new Color3<Rgb>(0.0f, _greenValue, 0.0f));
            });
        });
    }
}