using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using Silk.NET.GLFW;
using SilkGlfw = Silk.NET.GLFW.Glfw;

namespace Engine.Windowing.Glfw;

public unsafe class GlfwWindowManager : WindowManager
{
    private static GlfwWindowManager? _instance;
    private List<GlfwWindow> _windows = new();
    internal Silk.NET.GLFW.Glfw Api;

    internal (ErrorCode ErrorCode, string? Description) GetError()
    {
        ErrorCode error = Api.GetError(out byte* description);
        return (error, Marshal.PtrToStringAnsi((IntPtr)description));
    }

    internal string ErrorCodeWithDescription()
    {
        (ErrorCode ErrorCode, string Description) errorInfo = GetError();
        return $"{errorInfo.ErrorCode}: {errorInfo.Description}";
    }

    public static GlfwWindowManager? TryGetInstance()
        => TryCreateInstance().Instance;

    private static (GlfwWindowManager? Instance, string? ErrorMessage) TryCreateInstance()
    {
        if (_instance == null)
            _instance = new GlfwWindowManager();

        return (_instance, null);
    }

    public static GlfwWindowManager GetInstance()
    {
        (GlfwWindowManager? instance, string? error) = TryCreateInstance();
        if (instance == null)
            throw new InvalidOperationException(error);

        return instance;
    }

    private GlfwWindowManager()
    {
        Api = SilkGlfw.GetApi();
        Api.Init();
    }

    public override Window CreateWindow(string name)
    {
        Vector2i size = new Vector2i(800, 600);
        Api.WindowHint(WindowHintClientApi.ClientApi, ClientApi.NoApi);
        Api.WindowHint(WindowHintBool.Resizable, false);
        WindowHandle* window = Api.CreateWindow(size.X, size.Y, name, null, null);

        if (window == null)
            throw new InvalidOperationException($"Window creation failed. GLFW Error: {Api.GetError(out _)}");

        GlfwWindow win = new GlfwWindow(name, window, size, this);
        _windows.Add(win);
        return win;
    }

    public override IReadOnlyList<Window> Windows => _windows;

    public override void ProcessEvents()
    {
        Api.PollEvents();

        foreach (GlfwWindow window in _windows)
        {
            if (Api.WindowShouldClose(window.Handle))
            {
                Application.IsQuitRequested = true;
                break;
            }
        }
    }
}