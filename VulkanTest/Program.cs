using System;
using Vortice.ShaderCompiler;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace VulkanTest;

public static unsafe partial class Program
{
    public static void Main()
    {
        try
        {
            // // language=glsl
            // string vertexShader =
            //     """
            //     #version 450
            //     layout (location = 0) in vec3 inPos;
            //     layout (location = 1) in vec3 inColor;
            //     layout (binding = 0) uniform UBO 
            //     {
            //     	mat4 projectionMatrix;
            //     	mat4 modelMatrix;
            //     	mat4 viewMatrix;
            //     } ubo;
            //     layout (location = 0) out vec3 outColor;
            //     out gl_PerVertex 
            //     {
            //     	vec4 gl_Position;   
            //     };
            //     void main() 
            //     {
            //     	outColor = inColor;
            //     	gl_Position = ubo.projectionMatrix * ubo.viewMatrix * ubo.modelMatrix * vec4(inPos.xyz, 1.0);
            //     }
            //     """;
            //
            // using Compiler compiler = new Compiler();
            // using (var compilationResult = compiler.Compile(vertexShader, "main", ShaderKind.VertexShader))
            // {
            //     //vkCreateShaderModule(VkDevice, compilationResult.GetBytecode(), null, out VkShaderModule module).CheckResult();
            // }

            var testApp = new TestApp();
            testApp.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }
}