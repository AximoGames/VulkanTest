using Vortice.Vulkan;
using Vortice.ShaderCompiler;
using static Vortice.Vulkan.Vulkan;

namespace VulkanTest;

public unsafe class ShaderManager
{
    private readonly VkDevice _device;

    public ShaderManager(VkDevice device)
    {
        _device = device;
    }

    public VkShaderModule CreateShaderModuleFromCode(string shaderCode, ShaderKind shaderKind)
    {
        using Compiler compiler = new Compiler();
        using (var compilationResult = compiler.Compile(shaderCode, "main", shaderKind))
        {
            vkCreateShaderModule(_device, compilationResult.GetBytecode(), null, out VkShaderModule module).CheckResult();
            return module;
        }
    }
}