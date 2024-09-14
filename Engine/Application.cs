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
        windowManager.Application = this;
        _windowManagers.Add(windowManager);
    }

    protected abstract void Initialize();

    protected virtual void OnRenderFrame()
    {
    }

    public bool IsQuitRequested { get; internal set; }

    private long _frameIndex;

    public void Run()
    {
        Initialize();
        while (!IsQuitRequested)
        {
            var framIndex = _frameIndex++;
            ProcessEvents(framIndex);
            ProcessRenderFrame(framIndex);
        }
    }

    private void ProcessEvents(long frameIndex)
    {
        foreach (var windowManager in _windowManagers)
            windowManager.ProcessEvents();
    }

    private void ProcessRenderFrame(long frameIndex)
        => RenderFrame?.Invoke(new FrameEventArgs { FrameIndex = frameIndex });
}