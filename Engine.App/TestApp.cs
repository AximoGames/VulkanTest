using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Engine.Vulkan;
using OpenTK;
using OpenTK.Mathematics;

namespace Engine.App;

public class TestApp : Application
{
	private GraphicsDevice _graphicsDevice;
	private Pipeline _pipeline;
	private Buffer _vertexBuffer;
	private Buffer _indexBuffer;
	private float _greenValue = 0.0f;
	private bool EnableValidationLayers = true;
	
	public override string Name => "01-DrawTriangle";

	public Vertex[] Vertices =
	{
		new() { Position = new Vector2(-0.5f, -0.5f), Color = new Vector3(1.0f, 0.0f, 0.0f) },
		new() { Position = new Vector2(0.5f, -0.5f), Color = new Vector3(1.0f, 0.0f, 0.0f) },
		new() { Position = new Vector2(0.5f, 0.5f), Color = new Vector3(0.0f, 1.0f, 0.0f) },
		new() { Position = new Vector2(-0.5f, 0.5f), Color = new Vector3(0.0f, 0.0f, 1.0f) }
	};

	public ushort[] Indices =
	{
		0, 1, 2, 2, 3, 0,
	};

	protected override void Initialize()
	{
		var windowManager = SdlWindowManager.GetInstance();
		RegisterWindowManager(windowManager);
		var window = windowManager.CreateWindow(Name);
		RenderFrame += (e) => { OnRenderFrame(); };
		
		_graphicsDevice = new GraphicsDevice(VulkanGraphicsFactory.CreateVulkanGraphicsDevice(Name, EnableValidationLayers, window));
		_pipeline = _graphicsDevice.CreatePipeline(InitializePipeline);
		_graphicsDevice.InitializeResources(InitializeResources);
	}

	private void InitializePipeline(PipelineBuilder builder)
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
                }
            }
        };

        builder.ConfigureVertexLayout(vertexLayoutInfo);
        builder.ConfigureShader(vertexShaderCode, ShaderKind.VertexShader);
        builder.ConfigureShader(fragShaderCode, ShaderKind.FragmentShader);
   	}

	private void InitializeResources(ResourceAllocator allocator)
	{
		_vertexBuffer = allocator.CreateVertexBuffer(Vertices);
		_indexBuffer = allocator.CreateIndexBuffer(Indices);
	}

	protected override void OnRenderFrame()
	{
		try
		{
			_pipeline.RenderFrame(OnDraw);
		}
		catch (Exception ex)
		{
			Log.Error($"Unexpected error occurred: {ex.Message}");
		}
	}

	private void OnDraw(RenderContext context)
	{
		float g = _greenValue + 0.0003f;
		if (g > 1.0f)
			g = 0.0f;
		_greenValue = g;

		context.Clear(new Color3<Rgb>(0, 0, _greenValue));
		context.BindVertexBuffer(_vertexBuffer);
		context.BindIndexBuffer(_indexBuffer);
		context.DrawIndexed((uint)Indices.Length);
	}
}

