namespace Engine.Vulkan;

internal static class VulkanShaderKindExtensions
{
    public static Vortice.ShaderCompiler.ShaderKind ToVkShaderKind(this ShaderKind shaderKind)
    {
        return shaderKind switch
        {
            ShaderKind.Vertex => Vortice.ShaderCompiler.ShaderKind.VertexShader,
            ShaderKind.Fragment => Vortice.ShaderCompiler.ShaderKind.FragmentShader,
            ShaderKind.Compute => Vortice.ShaderCompiler.ShaderKind.ComputeShader,
            ShaderKind.Geometry => Vortice.ShaderCompiler.ShaderKind.GeometryShader,
            _ => throw new NotSupportedException($"ShaderKind {shaderKind} not supported.")
        };
    }
}