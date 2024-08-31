using System;
using Vortice.Vulkan;
using Vortice.ShaderCompiler;
using static Vortice.Vulkan.Vulkan;

namespace VulkanTest;

public unsafe class ShaderManager
{
    private readonly VulkanDevice _device;

    public ShaderManager(VulkanDevice device)
    {
        _device = device;
    }

    public ShaderModule CreateShaderModuleFromCode(string shaderCode, ShaderKind shaderKind)
    {
        using Compiler compiler = new Compiler();
        using (var compilationResult = compiler.Compile(shaderCode, "main", shaderKind))
        {
            vkCreateShaderModule(_device.LogicalDevice, compilationResult.GetBytecode(), null, out VkShaderModule module).CheckResult();
            return new ShaderModule(_device, module);
        }
    }
}