using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Vortice
{
    public abstract class Application
    {
        protected Application()
        {
        }

        public abstract string Name { get; }

        public Window? MainWindow { get; private set; }

        protected virtual void Initialize()
        {
            MainWindow = new Window("bla", 800, 600);
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