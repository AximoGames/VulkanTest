﻿using System;
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

            int x = CW_USEDEFAULT;
            int y = CW_USEDEFAULT;
            bool resizable = (flags & WindowFlags.Resizable) != WindowFlags.None;

            _windowWindowedStyle = WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU | WindowStyles.WS_MINIMIZEBOX | WindowStyles.WS_CLIPSIBLINGS | WindowStyles.WS_BORDER | WindowStyles.WS_DLGFRAME | WindowStyles.WS_THICKFRAME | WindowStyles.WS_GROUP | WindowStyles.WS_TABSTOP;

            if (resizable)
            {
                _windowWindowedStyle |= WindowStyles.WS_SIZEBOX | WindowStyles.WS_MAXIMIZEBOX;
            }

            _windowStyle = _windowWindowedStyle;

            RawRect windowRect = new RawRect(0, 0, width, height);

            // Adjust according to window styles
            AdjustWindowRectEx(ref windowRect, _windowStyle, false, WindowExStyles.WS_EX_OVERLAPPEDWINDOW);

            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;

            bool centerWindow = true;
            if (centerWindow)
            {
                if (windowWidth > 0 && windowHeight > 0)
                {
                    int screenWidth = GetSystemMetrics(SystemMetrics.SM_CXSCREEN);
                    int screenHeight = GetSystemMetrics(SystemMetrics.SM_CYSCREEN);

                    // Place the window in the middle of the screen.WS_EX_APPWINDOW
                    x = (screenWidth - windowWidth) / 2;
                    y = (screenHeight - windowHeight) / 2;
                }
            }

            IntPtr hwnd;
            //fixed (char* lpWndClassName = WndClassName)
            //{
            //    fixed (char* lpWindowName = Title)
            //    {
            //        hwnd = CreateWindowExW(
            //            (uint)WindowExStyles.WS_EX_OVERLAPPEDWINDOW,
            //            (ushort*)lpWndClassName,
            //            (ushort*)lpWindowName,
            //            (uint)_windowStyle,
            //        x,
            //        y,
            //        windowWidth,
            //        windowHeight,
            //        IntPtr.Zero,
            //        IntPtr.Zero,
            //        IntPtr.Zero,
            //        null);
            //    }
            //}

            GLFW.Init();
            var win = GLFW.CreateWindow(windowWidth, windowHeight, Title, null, null);
            if (win == default)
                throw new Exception("Windows creation failed");

            hwnd = GLFW.GetWin32Window(win);
            if (hwnd == IntPtr.Zero)
                throw new Exception("Can't get window handle");

            ShowWindow(hwnd, ShowWindowCommand.Normal);
            Handle = hwnd;

            GetClientRect(hwnd, out windowRect);
            Extent = new VkExtent2D(windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top);
        }

        public string Title { get; }
        public VkExtent2D Extent { get; }
        public IntPtr Handle { get; }
    }
}