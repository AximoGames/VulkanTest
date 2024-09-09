using System;
using System.Collections.Generic;
using Engine.Vulkan;
using OpenTK;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.App;

public class TestApp : Application
{
    private GraphicsDevice _graphicsDevice;
    private Pipeline _pipeline;
    private Buffer _vertexBuffer;
    private Buffer _indexBuffer;
    private Buffer _uniformBuffer;
    private float _greenValue = 0.0f;
    private bool EnableValidationLayers = true;

    private string[] suppressDebugMessages =
    [
        //"VUID-VkPresentInfoKHR-pImageIndices-01430",
    ];

    private Pass _renderPass;
    private Image _textureImage;

    public override string Name => "01-DrawTriangle";

    public Vertex[] Vertices =
    {
        new() { Position = new Vector2(-0.5f, -0.5f), Color = new Vector3(1.0f, 0.0f, 0.0f), TexCoord = new Vector2(0.0f, 0.0f) },
        new() { Position = new Vector2(0.5f, -0.5f), Color = new Vector3(1.0f, 0.0f, 0.0f), TexCoord = new Vector2(1.0f, 0.0f) },
        new() { Position = new Vector2(0.5f, 0.5f), Color = new Vector3(0.0f, 1.0f, 0.0f), TexCoord = new Vector2(1.0f, 1.0f) },
        new() { Position = new Vector2(-0.5f, 0.5f), Color = new Vector3(0.0f, 0.0f, 1.0f), TexCoord = new Vector2(0.0f, 1.0f) }
    };

    public ushort[] Indices =
    {
        0, 1, 2, 2, 3, 0,
    };

    private Sampler _sampler;

    protected override void Initialize()
    {
        var windowManager = SdlWindowManager.GetInstance();
        RegisterWindowManager(windowManager);
        var window = windowManager.CreateWindow(Name);
        RenderFrame += (e) => { OnRenderFrame(); };

        _graphicsDevice = new GraphicsDevice(VulkanGraphicsFactory.CreateVulkanGraphicsDevice(Name, EnableValidationLayers, window, suppressDebugMessages));
        _pipeline = _graphicsDevice.CreatePipeline(InitializePipeline);
        _graphicsDevice.InitializeResources(InitializeResources);
        InitializeRenderPass();
    }

    private Image<Rgba32> CreateGradientImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float r = (float)x / width;
                float g = (float)y / height;
                float b = 1.0f - ((float)x / width + (float)y / height) / 2;
                image[x, y] = new Rgba32(r, g, b);
            }
        }

        return image;
    }

    private void InitializePipeline(PipelineBuilder builder)
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

            //layout(set = 0, binding = 1) uniform sampler2D textureSampler;

            void main() {
                //vec4 textureColor = texture(textureSampler, fragTexCoord);
                //outColor = vec4(fragColor + ubo.colorFactor, 1.0) * textureColor;
                outColor = vec4(fragColor + ubo.colorFactor, 1.0);
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
                        // new()
                        // {
                        //     Binding = 1,
                        //     DescriptorType = DescriptorType.CombinedImageSampler,
                        //     DescriptorCount = 1,
                        //     StageFlags = ShaderStageFlags.Fragment
                        // }
                    }
                }
            }
        };

        builder.ConfigurePipelineLayout(layoutDescription);
    }

    private void InitializeResources(ResourceManager allocator)
    {
        _vertexBuffer = allocator.CreateVertexBuffer(Vertices);
        _indexBuffer = allocator.CreateIndexBuffer(Indices);
        _uniformBuffer = allocator.CreateUniformBuffer<float>();

        using (var gradientImage = CreateGradientImage(100, 100))
        {
            _textureImage = allocator.CreateImage(gradientImage);
        }

        _sampler = allocator.CreateSampler(new SamplerDescription
        {
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            MaxAnisotropy = 1.0f,
            CompareOp = CompareOp.Never,
            CompareEnable = false,
            BorderColor = BorderColor.FloatOpaqueBlack,
            UnnormalizedCoordinates = false,
            MipLodBias = 0.0f,
            MinLod = 0.0f,
            MaxLod = 0.0f
        });
    }

    private void InitializeRenderPass()
    {
        var swapchainRenderTarget = _graphicsDevice.GetSwapchainRenderTarget();

        _renderPass = _graphicsDevice.CreatePass(builder =>
        {
            var colorAttachmentDescription = new AttachmentDescription
            {
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                ImageLayout = ImageLayout.ColorAttachmentOptimal,
            };

            builder.ConfigureColorAttachment(colorAttachmentDescription);
            builder.SetRenderTarget(swapchainRenderTarget);
        });
    }

    protected override void OnRenderFrame()
    {
        float g = _greenValue + 0.0003f;
        if (g > 1.0f)
            g = 0.0f;
        _greenValue = g;

        // try
        // {
        _graphicsDevice.RenderFrame(frameContext =>
        {
            frameContext.UsePass(_renderPass, passContext =>
            {
                passContext.UsePipeline(_pipeline, drawContext =>
                {
                    drawContext.Clear(new Color3<Rgb>(0.0f, _greenValue, 0.0f));
                    drawContext.BindVertexBuffer(_vertexBuffer);
                    drawContext.BindIndexBuffer(_indexBuffer);
                    frameContext.ResourceManager.UpdateUniformBuffer(_uniformBuffer, _greenValue * 2);
                    drawContext.BindUniformBuffer(_uniformBuffer, 0, 0);
                    // drawContext.BindTexture(_textureImage, _sampler, 0, 1); // Bind the texture
                    drawContext.DrawIndexed((uint)Indices.Length);
                });
            });
        });
        // }
        // catch (Exception ex)
        // {
        //     Log.Error($"Unexpected error occurred: {ex.Message}");
        //     throw;
        // }
    }
}