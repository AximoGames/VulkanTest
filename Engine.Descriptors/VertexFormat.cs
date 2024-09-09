namespace Engine;

public enum VertexFormat
{
    Float32_2,
    Float32_3,
    Float32_4,
    // Add more formats as needed
}

public class PipelineLayoutDescription
{
    public List<DescriptorSetLayoutDescription> DescriptorSetLayouts { get; set; } = new List<DescriptorSetLayoutDescription>();
    public List<PushConstantRange> PushConstantRanges { get; set; } = new List<PushConstantRange>();
}

public class DescriptorSetLayoutDescription
{
    public List<DescriptorSetLayoutBinding> Bindings { get; set; } = new List<DescriptorSetLayoutBinding>();
}

public class DescriptorSetLayoutBinding
{
    public uint Binding { get; set; }
    public DescriptorType DescriptorType { get; set; }
    public uint DescriptorCount { get; set; }
    public ShaderStageFlags StageFlags { get; set; }
}

public enum DescriptorType
{
    UniformBuffer,
    UniformBufferDynamic,
    StorageBuffer,
    CombinedImageSampler,
    // Add more types as needed
}

public enum ShaderStageFlags
{
    Vertex = 1,
    Fragment = 2,
    Compute = 4,
    // Add more stages as needed
}

public class PushConstantRange
{
    public ShaderStageFlags StageFlags { get; set; }
    public uint Offset { get; set; }
    public uint Size { get; set; }
}