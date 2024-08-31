using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using Vortice.ShaderCompiler;
using OpenTK.Mathematics;

namespace VulkanTest;

public unsafe class TestApp : Application
{
#if DEBUG
    private static bool EnableValidationLayers = true;
#else
		private static bool EnableValidationLayers = false;
#endif

    [NotNull]
    private GraphicsDevice _graphicsDevice = default!;

    private float _greenValue = 0.0f;

    public override string Name => "01-DrawTriangle";

    public Vertex[] Vertices = new Vertex[]
    {
        new Vertex { position = new Vector2(-0.5f, -0.5f), color = new Vector3(1.0f, 0.0f, 0.0f) },
        new Vertex { position = new Vector2(0.5f, -0.5f), color = new Vector3(1.0f, 0.0f, 0.0f) },
        new Vertex { position = new Vector2(0.5f, 0.5f), color = new Vector3(0.0f, 1.0f, 0.0f) },
        new Vertex { position = new Vector2(-0.5f, 0.5f), color = new Vector3(0.0f, 0.0f, 1.0f) }
    };

    public ushort[] Indices =
    {
        0, 1, 2, 2, 3, 0,
    };

    private VulkanBuffer _vertexBuffer;
    private VulkanBuffer _indexBuffer;

    protected override void Initialize()
    {
        base.Initialize();
        _graphicsDevice = new GraphicsDevice(Name, EnableValidationLayers, MainWindow);
        _graphicsDevice.InitializePipeline(InitializePipeline);
    }

    protected void InitializePipeline(PipelineBuilder builder)
    {
        string vertexShaderCode =
            """
            #version 450

            layout(location = 0) in vec2 inPosition;
            layout(location = 1) in vec3 inColor;

            layout(location = 0) out vec3 fragColor;

            void main() {
                gl_Position = vec4(inPosition, 0.0, 1.0);
                fragColor = inColor;
            }
            """;

        string fragShaderCode =
            """
            #version 450

            layout(location = 0) in vec3 fragColor;

            layout(location = 0) out vec4 outColor;

            void main() {
                outColor = vec4(fragColor, 1.0);
            }
            """;

        _vertexBuffer = builder.BufferManager.CreateVertexBuffer(Vertices);
        _indexBuffer = builder.BufferManager.CreateIndexBuffer(Indices);

        builder.ConfigureShader(vertexShaderCode, ShaderKind.VertexShader);
        builder.ConfigureShader(fragShaderCode, ShaderKind.FragmentShader);
    }

    protected override void OnRenderFrame()
    {
        try
        {
            _graphicsDevice.RenderFrame(OnDraw);
        }
        catch (VkException ex)
        {
            Log.Error($"Vulkan error occurred: {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected error occurred: {ex.Message}");
        }
    }

    private void OnDraw(RenderContext renderContext)
    {
        float g = _greenValue + 0.0003f;
        if (g > 1.0f)
            g = 0.0f;
        _greenValue = g;

        renderContext.Clear(new VkClearColorValue(0, 0, _greenValue));
        renderContext.BindVertexBuffer(_vertexBuffer);
        renderContext.BindIndexBuffer(_indexBuffer);
        renderContext.DrawIndexed(6);
    }
}