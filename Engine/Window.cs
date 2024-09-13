using OpenTK.Mathematics;

namespace Engine;

public abstract class Window
{
    protected Window(string title, Vector2i clientSize, WindowManager windowManager)
    {
        // Handle = handle;
        ClientSize = clientSize;
        WindowManager = windowManager;
        Title = title;
    }

    public string Title { get; set; }
    // public IntPtr Handle { get; set; }
    public Vector2i ClientSize { get; set; }
    public WindowManager WindowManager { get; set; }
    public abstract ulong CreateVulkanSurfaceHandle(IntPtr vulkanInstanceHandle);

    public abstract IEnumerable<string> GetRequiredInstanceExtensions();
}