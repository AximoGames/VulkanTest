namespace Engine;

public abstract class WindowManager
{
    public Application Application { get; internal set; } = null!;
    public abstract Window CreateWindow(string name);
    public abstract IReadOnlyList<Window> Windows { get; }

    public IEnumerable<string> GetRequiredInstanceExtensions()
    {
        if (Windows.Count == 0)
            throw new InvalidOperationException("No windows have been created.");
        
        return Windows[0].GetRequiredInstanceExtensions();
    }

    public abstract void ProcessEvents();
}