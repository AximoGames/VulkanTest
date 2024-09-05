namespace Engine.Vulkan;

internal static class VulkanShaderKindExtensions
{
    public static Vortice.ShaderCompiler.ShaderKind ToVkShaderKind(this ShaderKind shaderKind)
    {
        return shaderKind switch
        {
            ShaderKind.VertexShader => Vortice.ShaderCompiler.ShaderKind.VertexShader,
            ShaderKind.FragmentShader => Vortice.ShaderCompiler.ShaderKind.FragmentShader,
            ShaderKind.ComputeShader => Vortice.ShaderCompiler.ShaderKind.ComputeShader,
            ShaderKind.GeometryShader => Vortice.ShaderCompiler.ShaderKind.GeometryShader,
            _ => throw new NotSupportedException($"ShaderKind {shaderKind} not supported.")
        };
    }
}