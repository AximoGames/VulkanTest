using System;
using System.Collections.Generic;
using Engine.ImGui;
using Engine.Vulkan;
using Engine.Windowing.Glfw;
using OpenTK;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.App;

public class ImGuiApp : Application
{
    public override string Name => "ImGui Hello World";

    private Device _device;
    private ImGuiRenderer _imGuiRenderer;
    private Pass _imGuiPass;

    protected override void Initialize()
    {
        var windowManager = GlfwWindowManager.GetInstance();
        RegisterWindowManager(windowManager);
        var window = windowManager.CreateWindow(Name);
        RenderFrame += OnRenderFrame;

        _device = new VulkanFactory()
            .CreateInstance(windowManager, Name)
            .CreateDevice(window);

        _imGuiRenderer = new ImGuiRenderer(_device);
        CreateImGuiPass();
    }

    private void CreateImGuiPass()
    {
        _imGuiPass = _device.CreatePass(builder =>
        {
            var colorAttachmentDescription = new AttachmentDescription
            {
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                ImageLayout = ImageLayout.ColorAttachmentOptimal,
            };

            builder.ConfigureColorAttachment(colorAttachmentDescription);
            builder.SetRenderTarget(_device.GetSwapchainRenderTarget());
        });
    }

    private void OnRenderFrame(FrameEventArgs args)
    {
        _imGuiRenderer.NewFrame(_device.GetSwapchainRenderTarget().Extent);

        ImGuiNET.ImGui.Begin("Hello, world!");
        ImGuiNET.ImGui.Text("This is an ImGui window.");
        ImGuiNET.ImGui.End();

        ImGuiNET.ImGui.Render();
        var drawData = ImGuiNET.ImGui.GetDrawData();
        
        _device.RenderFrame(frameContext =>
        {
            _imGuiRenderer.UpdateBuffers(frameContext.ResourceManager, drawData);

            frameContext.UsePass(_imGuiPass, passContext =>
            {
                passContext.Clear(new Color3<Rgb>(0.45f, 0.55f, 0.60f));
                _imGuiRenderer.Render(passContext, drawData);
            });
        });
    }
}