using System.Runtime.CompilerServices;

namespace Engine;

public class Pipeline
{
    internal BackendPipeline BackendPipeline { get; }

    internal Pipeline(BackendPipeline backendPipeline)
    {
        BackendPipeline = backendPipeline;
    }
}