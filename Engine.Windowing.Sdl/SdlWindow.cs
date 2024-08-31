using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using SDL2;

namespace Engine;

public class SdlWindow : Window
{
    internal SdlWindow(string title, IntPtr handle, Vector2i clientSize, WindowManager windowManager)
        : base(title, handle, clientSize, windowManager)
    {
    }

    public override ulong CreateVulkanSurfaceHandle(IntPtr vulkanInstanceHandle)
    {
        SDL.SDL_Vulkan_CreateSurface(Handle, vulkanInstanceHandle, out var surface);
        return surface;
    }

    public override IEnumerable<string> GetRequiredInstanceExtensions()
    {
        uint count = 0;
        SDL.SDL_Vulkan_GetInstanceExtensions(Handle, out count, IntPtr.Zero);

        if (count == 0)
        {
            return new string[0];
        }

        IntPtr[] pointers = new IntPtr[count];
        if (SDL.SDL_Vulkan_GetInstanceExtensions(Handle, out count, pointers) == SDL.SDL_bool.SDL_FALSE)
            throw new Exception("Failed to get Vulkan instance extensions.");

        string[] extensions = new string[count];
        for (int i = 0; i < count; i++)
            extensions[i] = Marshal.PtrToStringAnsi(pointers[i]);

        return extensions;
    }
}