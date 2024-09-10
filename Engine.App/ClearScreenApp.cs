using System;
using System.Collections.Generic;
using Engine.Vulkan;
using OpenTK;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.App;

public class ClearScreenApp : Application
{
    private GraphicsDevice _graphicsDevice;
    private Pipeline _drawPipeline;
    private float _greenValue = 0.0f;
    private bool EnableValidationLayers = true;

    private string[] suppressDebugMessages =
    [
        //"VUID-VkPresentInfoKHR-pImageIndices-01430",
    ];

    private Pass _drawPass;

    public override string Name => "01-DrawTriangle";

    protected override void Initialize()
    {
        var windowManager = SdlWindowManager.GetInstance();
        RegisterWindowManager(windowManager);
        var window = windowManager.CreateWindow(Name);
        RenderFrame += (e) => { OnRenderFrame(); };

        _graphicsDevice = new GraphicsDevice(VulkanGraphicsFactory.CreateVulkanGraphicsDevice(Name, EnableValidationLayers, window, suppressDebugMessages));

        CreatePipelines();
        CreatePasses();
    }

    private void CreatePipelines()
    {
        _drawPipeline = _graphicsDevice.CreatePipeline(ConfigureDrawPipeline);
    }

    private Image<Bgra32> CreateGradientImage(int width, int height)
    {
        var image = new Image<Bgra32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float r = (float)x / width;
                float g = (float)y / height;
                float b = 1.0f - ((float)x / width + (float)y / height) / 2;
                image[x, y] = new Bgra32((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
            }
        }

        return image;
    }

    private void ConfigureDrawPipeline(PipelineBuilder builder)
    {
        string vertexShaderCode =
            // lang=glsl
            """
            #version 450

            layout(location = 0) in vec2 inPosition;
            layout(location = 1) in vec3 inColor;
            layout(location = 2) in vec2 inTexCoord;

            layout(location = 0) out vec3 fragColor;
            layout(location = 1) out vec2 fragTexCoord;

            void main() {
                gl_Position = vec4(inPosition, 0.0, 1.0);
                fragColor = inColor;
                fragTexCoord = inTexCoord;
            }
            """;

        string fragShaderCode =
            // lang=glsl
            """
            #version 450

            layout(location = 0) in vec3 fragColor;
            layout(location = 1) in vec2 fragTexCoord;

            layout(location = 0) out vec4 outColor;

            layout(set = 0, binding = 0) uniform UniformBufferObject {
                float colorFactor;
            } ubo;

            layout(set = 1, binding = 0) uniform sampler2D image;

            void main() {
                vec4 textureColor = texture(image, fragTexCoord);
                outColor = vec4(fragColor + ubo.colorFactor, 1.0) * textureColor;
                //outColor = vec4(fragColor + ubo.colorFactor, 1.0);
            }
            """;

        var vertexLayoutInfo = new VertexLayoutInfo
        {
            BindingDescription = new VertexInputBindingDescription
            {
                Binding = 0,
                Stride = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Vertex>(),
                InputRate = VertexInputRate.Vertex
            },
            AttributeDescriptions = new List<VertexInputAttributeDescription>
            {
                new()
                {
                    Binding = 0,
                    Location = 0,
                    Format = VertexFormat.Float32_2,
                    Offset = 0
                },
                new()
                {
                    Binding = 0,
                    Location = 1,
                    Format = VertexFormat.Float32_3,
                    Offset = (uint)System.Runtime.InteropServices.Marshal.SizeOf<Vector2>()
                },
                new()
                {
                    Binding = 0,
                    Location = 2,
                    Format = VertexFormat.Float32_2,
                    Offset = (uint)(System.Runtime.InteropServices.Marshal.SizeOf<Vector2>() + System.Runtime.InteropServices.Marshal.SizeOf<Vector3>())
                }
            }
        };

        builder.ConfigureVertexLayout(vertexLayoutInfo);
        builder.ConfigureShader(vertexShaderCode, ShaderKind.VertexShader);
        builder.ConfigureShader(fragShaderCode, ShaderKind.FragmentShader);

        var layoutDescription = new PipelineLayoutDescription
        {
            DescriptorSetLayouts = new List<DescriptorSetLayoutDescription>
            {
                new()
                {
                    Bindings = new List<DescriptorSetLayoutBinding>
                    {
                        new()
                        {
                            Binding = 0,
                            DescriptorType = DescriptorType.UniformBufferDynamic,
                            DescriptorCount = 1,
                            StageFlags = ShaderStageFlags.Fragment
                        },
                    },
                },
                new()
                {
                    Bindings = new List<DescriptorSetLayoutBinding>
                    {
                        new()
                        {
                            Binding = 0,
                            DescriptorType = DescriptorType.CombinedImageSampler,
                            DescriptorCount = 1,
                            StageFlags = ShaderStageFlags.Fragment
                        }
                    }
                }
            },
        };
        builder.ConfigurePipelineLayout(layoutDescription);
    }

    private void CreatePasses()
    {
        _drawPass = _graphicsDevice.CreatePass(builder =>
        {
            var colorAttachmentDescription = new AttachmentDescription
            {
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                ImageLayout = ImageLayout.ColorAttachmentOptimal,
            };

            builder.ConfigureColorAttachment(colorAttachmentDescription);
            builder.SetRenderTarget(_graphicsDevice.GetSwapchainRenderTarget());
        });
    }

    protected override void OnRenderFrame()
    {
        float g = _greenValue + 0.0003f;
        if (g > 1.0f)
            g = 0.0f;
        _greenValue = g;

        _graphicsDevice.RenderFrame(frameContext =>
        {
            frameContext.UsePass(_drawPass, passContext =>
            {
                passContext.UsePipeline(_drawPipeline, drawContext =>
                {
                    drawContext.Clear(new Color3<Rgb>(0.0f, _greenValue, 0.0f));
                });
            });
        });
    }
}