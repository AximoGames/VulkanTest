namespace Engine;

public class SamplerDescription
{
    public Filter MinFilter { get; set; }
    public Filter MagFilter { get; set; }
    public SamplerAddressMode AddressModeU { get; set; }
    public SamplerAddressMode AddressModeV { get; set; }
    public SamplerAddressMode AddressModeW { get; set; }
    public float MipLodBias { get; set; }
    public bool AnisotropyEnable { get; set; }
    public float MaxAnisotropy { get; set; }
    public bool CompareEnable { get; set; }
    public CompareOp CompareOp { get; set; }
    public float MinLod { get; set; }
    public float MaxLod { get; set; }
    public BorderColor BorderColor { get; set; }
    public bool UnnormalizedCoordinates { get; set; }
}