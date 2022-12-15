using Newtonsoft.Json;

namespace ClothDemo.Effects.DataStructures;

public struct CapePhysicalProperties
{
    [JsonConstructor]
    public CapePhysicalProperties(float drag, float segmentDragVariance, float gravityFactor,
        CapeWindProperties windProperties,
        float stretchThreshold)
    {
        Drag = drag;
        SegmentDragVariance = segmentDragVariance;
        GravityFactor = gravityFactor;
        WindProperties = windProperties;
        StretchThreshold = stretchThreshold;
    }

    public float Drag { get; }
    public float SegmentDragVariance { get; }
    public float GravityFactor { get; }
    public CapeWindProperties WindProperties { get; }
    public float StretchThreshold { get; }
}