using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Windowing.Desktop;

namespace Vortice
{
    public abstract class Application
    {
        protected Application()
        {
        }

        public abstract string Name { get; }

        public GameWindow? MainWindow { get; private set; }

        protected virtual void Initialize()
        {
            MainWindow = new GameWindow(new GameWindowSettings(), new NativeWindowSettings());
        }

        protected virtual void OnTick()
        {
        }

        public void Run()
        {
            Initialize();
            while (true)
            {
                OnTick();
            }
        }

        protected virtual void OnActivated()
        {
        }

        protected virtual void OnDeactivated()
        {
        }

        protected virtual void OnDraw(int width, int height)
        {

        }

    }
}