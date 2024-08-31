using System;
using Vortice.Vulkan;
using Vortice.ShaderCompiler;
using static Vortice.Vulkan.Vulkan;

namespace Engine.Vulkan;

public unsafe class ShaderManager
{
    private readonly VulkanDevice _device;

    public ShaderManager(VulkanDevice device)
    {
        _device = device;
    }

    public VulkanShaderModule CreateShaderModuleFromCode(string shaderCode, ShaderKind shaderKind)
    {
        using Compiler compiler = new Compiler();
        using (var compilationResult = compiler.Compile(shaderCode, "main", shaderKind.ToVkShaderKind()))
        {
            vkCreateShaderModule(_device.LogicalDevice, compilationResult.GetBytecode(), null, out VkShaderModule module).CheckResult();
            return new VulkanShaderModule(_device, module);
        }
    }
}

public static class ShaderKindExtensions
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