using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;

namespace Vortice
{
    public class MyGameWindow : GameWindow
    {
        public MyGameWindow(string name) :
         base(new GameWindowSettings { IsMultiThreaded = true }, new NativeWindowSettings { Title = name, API = ContextAPI.NoAPI })
        {
        }

        public override void Run()
        {
            while (true)
            {
                OnRenderFrame(new FrameEventArgs());
            }
        }
    }

    public abstract class Application
    {
        protected Application()
        {
        }

        public abstract string Name { get; }

        [NotNull]
        public MyGameWindow MainWindow { get; private set; } = default!;

        protected virtual void Initialize()
        {
            MainWindow = new MyGameWindow(Name);
            MainWindow.RenderFrame += (e) =>
            {
                OnRenderFrame();
            };
        }

        protected virtual void OnRenderFrame()
        {
        }

        public void Run()
        {
            Initialize();
            MainWindow.Run();
        }

    }
}