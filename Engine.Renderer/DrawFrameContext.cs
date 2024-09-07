namespace Engine;

public class DrawFrameContext
{
    public void UsePass(Pass pass, Action<UsePassContext> draw)
    {
        draw(new UsePassContext());
    }
}