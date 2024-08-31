using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Engine;

public abstract class Application
{
    public event Action<FrameEventArgs> RenderFrame;

    protected Application()
    {
    }

    public abstract string Name { get; }

    private List<WindowManager> _windowManagers = new();

    public void RegisterWindowManager(WindowManager windowManager)
    {
        _windowManagers.Add(windowManager);
    }

    protected virtual void Initialize()
    {
        // Window = new OpenTkGameWindow(Name);
        // MainWindow.RenderFrame += (e) => { OnRenderFrame(); };
    }

    protected virtual void OnRenderFrame()
    {
    }

    public void Run()
    {
        Initialize();
        while (true)
        {
            RenderFrame?.Invoke(new FrameEventArgs());
        }
    }
}