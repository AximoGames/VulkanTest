using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using Silk.NET.Core.Native;
using Silk.NET.GLFW;

namespace Engine.Windowing.Glfw;

public unsafe class GlfwWindow : Window
{
    internal WindowHandle* Handle;

    internal GlfwWindow(string title, WindowHandle* hande, Vector2i clientSize, WindowManager windowManager)
        : base(title, clientSize, windowManager)
    {
        Handle = hande;
    }

    public override ulong CreateVulkanSurfaceHandle(IntPtr vulkanInstanceHandle)
    {
        ErrorCode e = ((GlfwWindowManager)WindowManager).Api.GetError(out _);
        VkNonDispatchableHandle ptr;
        if (((GlfwWindowManager)WindowManager).Api.CreateWindowSurface(new VkHandle(vulkanInstanceHandle), Handle, null, &ptr) != 0)
            throw new Exception($"Failed to create Vulkan surface. Error: {((GlfwWindowManager)WindowManager).ErrorCodeWithDescription()}");

        return ptr.Handle;
    }

    public override IEnumerable<string> GetRequiredInstanceExtensions()
    {
        byte** extensions = ((GlfwWindowManager)WindowManager).Api.GetRequiredInstanceExtensions(out uint count);
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
        {
            names[i] = Marshal.PtrToStringAnsi((IntPtr)extensions[i]);
        }

        return names;
    }
}