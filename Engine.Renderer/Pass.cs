namespace Engine;

public class Pass
{
    internal Pass(BackendPass backendPass)
    {
        BackendPass = backendPass;
    }

    internal BackendPass BackendPass { get; }
}