namespace Engine;

public class Sampler
{
    internal BackendSampler BackendSampler { get; }

    internal Sampler(BackendSampler backendSampler)
    {
        BackendSampler = backendSampler;
    }
}