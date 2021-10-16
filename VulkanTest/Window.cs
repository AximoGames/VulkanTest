using System;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vortice.Vulkan;
using Vortice.Win32;

namespace Vortice
{
    public unsafe sealed class Window
    {
        internal static readonly string WndClassName = "VorticeWindow";
        //private readonly WindowStyles _windowFullscreenStyle = WindowStyles.WS_CLIPSIBLINGS | WindowStyles.WS_GROUP | WindowStyles.WS_TABSTOP;
        private unsafe IntPtr GlfwWindow;

        public unsafe Window(string title, int width, int height, WindowFlags flags = WindowFlags.None)
        {
            Title = title;

            IntPtr hwnd;

            GLFW.Init();
            var win = GLFW.CreateWindow(width, height, Title, null, null);
            if (win == default)
                throw new Exception("Windows creation failed");
            GlfwWindow = (IntPtr)win;

            hwnd = GLFW.GetWin32Window(win);
            if (hwnd == IntPtr.Zero)
                throw new Exception("Can't get window handle");

            // TODO: Frame Size (border, decoration, ...)

            GLFW.ShowWindow(win);
            Handle = hwnd;
            //GLFW.GetWindowFrameSize(win, out var a, out var b, out var c, out var d);
            GLFW.GetWindowSize(win, out var w, out var h);
            Extent = new VkExtent2D(w, h);
        }

        public string Title { get; }
        public VkExtent2D Extent { get; }
        public IntPtr Handle { get; }
    }
}