using System.Runtime.InteropServices;
using ImGuiNET;
using OpenTK.Mathematics;
using Engine;

namespace Engine.ImGui;

public unsafe class ImGuiRenderer
{
    private Device _device;
    private Pipeline _pipeline;
    private Buffer _vertexBuffer;
    private Buffer _indexBuffer;
    private Buffer _uniformBuffer;
    private Image _fontTexture;
    private Sampler _fontSampler;

    public ImGuiRenderer(Device device)
    {
        _device = device;
        ImGuiNET.ImGui.CreateContext();
        CreateResources();
    }

    private void CreateResources()
    {
        // Create pipeline
        _pipeline = _device.CreatePipeline(builder =>
        {
            builder.ConfigureShader(ImGuiVertexShader, ShaderKind.Vertex);
            builder.ConfigureShader(ImGuiFragmentShader, ShaderKind.Fragment);

            // Configure vertex layout
            var vertexLayoutInfo = new VertexLayoutInfo
            {
                BindingDescription = new VertexInputBindingDescription
                {
                    Binding = 0,
                    Stride = (uint)Marshal.SizeOf<ImDrawVert>(),
                    InputRate = VertexInputRate.Vertex
                },
                AttributeDescriptions = new List<VertexInputAttributeDescription>
                {
                    new()
                    {
                        Binding = 0,
                        Location = 0,
                        Format = VertexFormat.Float32_2,
                        Offset = (uint)Marshal.OffsetOf<ImDrawVert>("pos")
                    },
                    new()
                    {
                        Binding = 0,
                        Location = 1,
                        Format = VertexFormat.Float32_2,
                        Offset = (uint)Marshal.OffsetOf<ImDrawVert>("uv")
                    },
                    new()
                    {
                        Binding = 0,
                        Location = 2,
                        Format = VertexFormat.Float8_4_Normalized,
                        Offset = (uint)Marshal.OffsetOf<ImDrawVert>("col")
                    }
                }
            };
            builder.ConfigureVertexLayout(vertexLayoutInfo);

            // Configure pipeline layout
            var layoutDescription = new PipelineLayoutDescription
            {
                DescriptorSetLayouts = new List<DescriptorSetLayoutDescription>
                {
                    new()
                    {
                        Bindings = new List<DescriptorSetLayoutBinding>
                        {
                            new(binding: 0, descriptorType: DescriptorType.UniformBufferDynamic, descriptorCount: 1, stageFlags: ShaderStageFlags.Vertex),
                            new(binding: 1, descriptorType: DescriptorType.CombinedImageSampler, descriptorCount: 1, stageFlags: ShaderStageFlags.Fragment)
                        }
                    }
                }
            };
            builder.ConfigurePipelineLayout(layoutDescription);
        });

        // Create buffers and textures
        _device.InitializeResources(resources =>
        {
            _vertexBuffer = resources.CreateVertexBuffer<ImDrawVert>(1000);
            _indexBuffer = resources.CreateIndexBuffer<ushort>(1000);
            _uniformBuffer = resources.CreateUniformBuffer<Matrix4>();
            _fontTexture = CreateFontTexture(resources);
            _fontSampler = resources.CreateSampler(new SamplerDescription
            {
                MinFilter = Filter.Linear,
                MagFilter = Filter.Linear,
                AddressModeU = SamplerAddressMode.Repeat,
                AddressModeV = SamplerAddressMode.Repeat,
                AddressModeW = SamplerAddressMode.Repeat
            });
        });
    }

    private Image CreateFontTexture(ResourceManager resources)
    {
        ImGuiNET.ImGui.GetIO().Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        var pixelData = new byte[width * height * bytesPerPixel];
        Marshal.Copy(pixels, pixelData, 0, pixelData.Length);
        return resources.CreateImage(pixelData, new Vector2i(width, height));
    }

    public void Render(UsePassContext passContext, ImDrawDataPtr drawData)
    {
        passContext.UsePipeline(_pipeline, pipelineContext =>
        {
            // Update buffers and bind resources
            pipelineContext.BindVertexBuffer(_vertexBuffer);
            pipelineContext.BindIndexBuffer(_indexBuffer);
            pipelineContext.BindUniformBuffer(_uniformBuffer, 0, 0, [0]);
            pipelineContext.BindImage(_fontTexture, _fontSampler, 0, 1, [0]);

            // Draw ImGui
            for (int i = 0; i < drawData.CmdListsCount; i++)
            {
                var cmdList = drawData.CmdLists[i];
                for (int j = 0; j < cmdList.CmdBuffer.Size; j++)
                {
                    var drawCmd = cmdList.CmdBuffer[j];
                    pipelineContext.DrawIndexed(drawCmd.ElemCount, 1, drawCmd.IdxOffset, (int)drawCmd.VtxOffset, 0);
                }
            }
        });
    }

    public void UpdateBuffers(ResourceManager resources, ImDrawDataPtr drawData)
    {
        // Update vertex and index buffers
        if (drawData.TotalVtxCount > 0)
        {
            // Resize vertex buffer if necessary
            if (_vertexBuffer.Size < drawData.TotalVtxCount * sizeof(ImDrawVert))
            {
                _vertexBuffer.Dispose();
                _vertexBuffer = resources.CreateVertexBuffer<ImDrawVert>(drawData.TotalVtxCount);
            }

            // Resize index buffer if necessary
            if (_indexBuffer.Size < drawData.TotalIdxCount * sizeof(ushort))
            {
                _indexBuffer.Dispose();
                _indexBuffer = resources.CreateIndexBuffer<ushort>(drawData.TotalIdxCount);
            }

            // Update vertex and index buffers
            int vertexOffset = 0;
            int indexOffset = 0;
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];

                // Copy vertex data
                var vertexData = new Span<ImDrawVert>((void*)cmdList.VtxBuffer.Data, cmdList.VtxBuffer.Size);
                resources.CopyBuffer(vertexData, 0, _vertexBuffer, vertexOffset, cmdList.VtxBuffer.Size);

                // Copy index data
                var indexData = new Span<ushort>((void*)cmdList.IdxBuffer.Data, cmdList.IdxBuffer.Size);
                resources.CopyBuffer(indexData, 0, _indexBuffer, indexOffset, cmdList.IdxBuffer.Size);

                vertexOffset += cmdList.VtxBuffer.Size;
                indexOffset += cmdList.IdxBuffer.Size;
            }
        }

        // Update uniform buffer with projection matrix
        var io = ImGuiNET.ImGui.GetIO();
        Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
        resources.UpdateUniformBuffer(_uniformBuffer, mvp);
    }

    public void NewFrame(Vector2i windowSize)
    {
        ImGuiNET.ImGui.GetIO().DisplaySize = new System.Numerics.Vector2(windowSize.X, windowSize.Y);
        ImGuiNET.ImGui.NewFrame();
    }

    private static readonly string ImGuiVertexShader = @"
#version 450
layout(location = 0) in vec2 inPosition;
layout(location = 1) in vec2 inUV;
layout(location = 2) in vec4 inColor;

layout(location = 0) out vec2 outUV;
layout(location = 1) out vec4 outColor;

layout(set = 0, binding = 0) uniform UniformBufferObject {
    mat4 projectionMatrix;
} ubo;

void main() {
    gl_Position = ubo.projectionMatrix * vec4(inPosition.xy, 0.0, 1.0);
    outUV = inUV;
    outColor = inColor;
}
";

    private static readonly string ImGuiFragmentShader = @"
#version 450
layout(location = 0) in vec2 inUV;
layout(location = 1) in vec4 inColor;

layout(location = 0) out vec4 outColor;

layout(set = 0, binding = 1) uniform sampler2D fontSampler;

void main() {
    outColor = inColor * texture(fontSampler, inUV);
}
";
}