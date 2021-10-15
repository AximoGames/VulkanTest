using System;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vortice.Vulkan;
using Vortice.Win32;
using static Vortice.Win32.User32;

namespace Vortice
{
    public sealed class Window
    {
        internal static readonly string WndClassName = "VorticeWindow";
        private const int CW_USEDEFAULT = unchecked((int)0x80000000);
        private WindowStyles _windowStyle = 0;
        private WindowStyles _windowWindowedStyle = 0;
        //private readonly WindowStyles _windowFullscreenStyle = WindowStyles.WS_CLIPSIBLINGS | WindowStyles.WS_GROUP | WindowStyles.WS_TABSTOP;

        public unsafe Window(string title, int width, int height, WindowFlags flags = WindowFlags.None)
        {
            Title = title;

            IntPtr hwnd;

            GLFW.Init();
            var win = GLFW.CreateWindow(width, height, Title, null, null);
            if (win == default)
                throw new Exception("Windows creation failed");

            hwnd = GLFW.GetWin32Window(win);
            if (hwnd == IntPtr.Zero)
                throw new Exception("Can't get window handle");

            // TODO: Frame Size (border, decoration, ...)

            ShowWindow(hwnd, ShowWindowCommand.Normal);
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