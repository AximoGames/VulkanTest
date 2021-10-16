using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using OpenTK.Windowing.Desktop;

namespace Vortice
{
    public abstract class Application
    {
        protected Application()
        {
        }

        public abstract string Name { get; }

        [NotNull]
        public GameWindow MainWindow { get; private set; } = default!;

        protected virtual void Initialize()
        {
            MainWindow = new GameWindow(new GameWindowSettings { IsMultiThreaded = true, }, new NativeWindowSettings { Title = Name });
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