using ClothDemo.Physics;

namespace ClothDemo.Effects.DataStructures;

public class CapeData
{
    public int CapeXOffset { get; set; }
    public int CapeYOffset { get; set; }
    public ClothDimensions Dimensions { get; set; }
    public CapeAnchor Anchor { get; set; }
    public CapePhysicalProperties PhysicalProperties { get; set; }
    public float DefaultDamping { get; set; }
    public int ConstraintPasses { get; set; }
    public CapeShader Shader { get; set; }
}