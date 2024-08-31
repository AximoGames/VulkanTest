using OpenTK.Mathematics;
using SDL2;

namespace Engine;

public class SdlWindowManager : WindowManager
{
    private static SdlWindowManager? _instance;

    private List<Window> _windows = new();

    public static SdlWindowManager? TryGetInstance()
        => TryCreateInstance().Instance;

    private static (SdlWindowManager? Instance, string? ErrorMessage) TryCreateInstance()
    {
        if (_instance == null)
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            {
                var error = $"SDL initialization failed. SDL Error: {SDL.SDL_GetError()}";
                Console.WriteLine(error);
                return (null, error);
            }
            else
            {
                _instance = new SdlWindowManager();
            }
        }

        return (_instance, null);
    }

    public static SdlWindowManager GetInstance()
    {
        var (instance, error) = TryCreateInstance();
        if (instance == null)
            throw new InvalidOperationException(error);

        return instance;
    }

    private SdlWindowManager()
    {
    }

    public override Window CreateWindow(string name)
    {
        var size = new Vector2i(800, 600);
        IntPtr window = SDL.SDL_CreateWindow(
            name,
            SDL.SDL_WINDOWPOS_UNDEFINED,
            SDL.SDL_WINDOWPOS_UNDEFINED,
            size.X,
            size.Y,
            SDL.SDL_WindowFlags.SDL_WINDOW_VULKAN | SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN
        );

        if (window == IntPtr.Zero)
            throw new InvalidOperationException($"Window creation failed. SDL Error: {SDL.SDL_GetError()}");

        var win = new SdlWindow(name, window, size, this);
        _windows.Add(win);
        return win;
    }

    public override IReadOnlyList<Window> Windows => _windows;

    public override void ProcessEvents()
    {
        SDL.SDL_Event e;
        while (SDL.SDL_PollEvent(out e) != 0)
        {
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_QUIT:
                    Application.IsQuitRequested = true;
                    break;
            }
        }
    }
}