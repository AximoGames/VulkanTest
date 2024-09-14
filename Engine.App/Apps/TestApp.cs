using System;
using System.Collections.Generic;
using Engine.Vulkan;
using Engine.Windowing.Glfw;
using OpenTK;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Engine.App;

public class TestApp : Application
{
    public override string Name => "01-DrawTriangle";
    private bool _enableValidationLayers = true;
    private string[] _suppressDebugMessages =
    [
        //"VUID-VkPresentInfoKHR-pImageIndices-01430",
    ];

    private Device _device;
    private Pipeline _drawPipeline;
    private Buffer _vertexBuffer;
    private Buffer _indexBuffer;
    private Buffer _uniformBuffer;
    private float _greenValue = 0.0f;


    private Pass _drawPass;
    private Image _image;
    private Pass _postProcessPass;
    private Pipeline _postProcessPipeline;
    private RenderTarget _intermediateRenderTarget;


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
        var windowManager = GlfwWindowManager.GetInstance();
        RegisterWindowManager(windowManager);
        var window = windowManager.CreateWindow(Name);
        RenderFrame += e => { OnRenderFrame(); };

        _device = new VulkanFactory()
            .CreateInstance(windowManager, Name, _enableValidationLayers, _suppressDebugMessages)
            .CreateDevice(window);

        _device.InitializeResources(InitializeResources);

        CreatePipelines();
        CreatePasses();
    }

    private void CreatePipelines()
    {
        _drawPipeline = _device.CreatePipeline(ConfigureDrawPipeline);
        _postProcessPipeline = _device.CreatePipeline(ConfigurePostProcessPipeline);
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
        builder.ConfigureShader(vertexShaderCode, ShaderKind.Vertex);
        builder.ConfigureShader(fragShaderCode, ShaderKind.Fragment);

        var layoutDescription = new PipelineLayoutDescription
        {
            DescriptorSetLayouts = new List<DescriptorSetLayoutDescription>
            {
                new()
                {
                    Bindings = new List<DescriptorSetLayoutBinding>
                    {
                        new(binding: 0, descriptorType: DescriptorType.UniformBufferDynamic, descriptorCount: 1, stageFlags: ShaderStageFlags.Fragment),
                    },
                },
                new()
                {
                    Bindings = new List<DescriptorSetLayoutBinding>
                    {
                        new(binding: 0, descriptorType: DescriptorType.CombinedImageSampler, descriptorCount: 1, stageFlags: ShaderStageFlags.Fragment)
                    }
                }
            },
        };
        builder.ConfigurePipelineLayout(layoutDescription);
    }

    private void ConfigurePostProcessPipeline(PipelineBuilder builder)
    {
        string vertexShaderCode =
            // lang=glsl
            """
            #version 450

            layout(location = 0) out vec2 fragTexCoord;

            vec2 positions[6] = vec2[](
                vec2(-1.0, -1.0),
                vec2(1.0, -1.0),
                vec2(-1.0, 1.0),
                vec2(-1.0, 1.0),
                vec2(1.0, -1.0),
                vec2(1.0, 1.0)
            );

            vec2 texCoords[6] = vec2[](
                vec2(0.0, 0.0),
                vec2(1.0, 0.0),
                vec2(0.0, 1.0),
                vec2(0.0, 1.0),
                vec2(1.0, 0.0),
                vec2(1.0, 1.0)
            );

            void main() {
                gl_Position = vec4(positions[gl_VertexIndex], 0.0, 1.0);
                fragTexCoord = texCoords[gl_VertexIndex];
            }
            """;

        string fragShaderCode =
            // lang=glsl
            """
            #version 450

            layout(location = 0) in vec2 fragTexCoord;
            layout(location = 0) out vec4 outColor;

            layout(set = 0, binding = 0) uniform sampler2D inputImage;

            void main() {
                vec4 color = texture(inputImage, fragTexCoord);
                float gray = dot(color.rgb, vec3(0.299, 0.587, 0.114));
                outColor = vec4(gray, gray, gray, color.a);
            }
            """;

        builder.ConfigureShader(vertexShaderCode, ShaderKind.Vertex);
        builder.ConfigureShader(fragShaderCode, ShaderKind.Fragment);

        // Add this new section for ConfigureVertexLayout
        var vertexLayoutInfo = new VertexLayoutInfo
        {
            BindingDescription = new VertexInputBindingDescription
            {
                Binding = 0,
                Stride = 0, // We're using gl_VertexIndex, so no vertex data
                InputRate = VertexInputRate.Vertex
            },
            AttributeDescriptions = new List<VertexInputAttributeDescription>()
            // No attribute descriptions needed as we're using gl_VertexIndex
        };
        builder.ConfigureVertexLayout(vertexLayoutInfo);

        var layoutDescription = new PipelineLayoutDescription
        {
            DescriptorSetLayouts = new List<DescriptorSetLayoutDescription>
            {
                new()
                {
                    Bindings = new List<DescriptorSetLayoutBinding>
                    {
                        new(binding: 0, descriptorType: DescriptorType.CombinedImageSampler, descriptorCount: 1, stageFlags: ShaderStageFlags.Fragment)
                    }
                }
            },
        };
        builder.ConfigurePipelineLayout(layoutDescription);
    }

    private void InitializeResources(ResourceManager allocator)
    {
        _vertexBuffer = allocator.CreateVertexBuffer(Vertices);
        _indexBuffer = allocator.CreateIndexBuffer(Indices);
        _uniformBuffer = allocator.CreateUniformBuffer<float>();

        using (var gradientImage = CreateGradientImage(100, 100))
            _image = allocator.CreateImage(gradientImage);

        _sampler = allocator.CreateSampler(new SamplerDescription
        {
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
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

        _intermediateRenderTarget = allocator.CreateImageRenderTarget(_device.GetSwapchainRenderTarget().Extent);
    }

    private void CreatePasses()
    {
        CreateDrawPass();
        CreatePostProcessPass();
    }

    private void CreateDrawPass()
    {
        _drawPass = _device.CreatePass(builder =>
        {
            var colorAttachmentDescription = new AttachmentDescription
            {
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                ImageLayout = ImageLayout.ColorAttachmentOptimal,
            };

            builder.ConfigureColorAttachment(colorAttachmentDescription);
            builder.SetRenderTarget(_intermediateRenderTarget);
        });
    }

    private void CreatePostProcessPass()
    {
        var swapchainRenderTarget = _device.GetSwapchainRenderTarget();

        _postProcessPass = _device.CreatePass(builder =>
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

        _device.RenderFrame(frameContext =>
        {
            frameContext.ResourceManager.UpdateUniformBuffer(_uniformBuffer, _greenValue * 2);

            frameContext.UsePass(_drawPass, passContext =>
            {
                passContext.Clear(new Color3<Rgb>(0.0f, _greenValue, 0.0f));
                passContext.UsePipeline(_drawPipeline, drawContext =>
                {
                    drawContext.BindVertexBuffer(_vertexBuffer);
                    drawContext.BindIndexBuffer(_indexBuffer);
                    drawContext.BindUniformBuffer(_uniformBuffer, 0, 0);
                    drawContext.BindImage(_image, _sampler, 1, 0);
                    drawContext.DrawIndexed((uint)Indices.Length);
                });
            });

            frameContext.UsePass(_postProcessPass, passContext =>
            {
                passContext.Clear(new Color3<Rgb>(0.0f, 0.0f, 0.0f));
                passContext.UsePipeline(_postProcessPipeline, drawContext =>
                {
                    drawContext.BindImage(_intermediateRenderTarget, _sampler, 0, 0);
                    drawContext.Draw(6);
                });
            });
        });
    }
}