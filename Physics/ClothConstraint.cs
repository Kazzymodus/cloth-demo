namespace ClothDemo.Physics;

public struct ClothConstraint
{
    public ClothConstraint(short segmentOneIndex, short segmentTwoIndex, float length)
    {
        SegmentOneIndex = segmentOneIndex;
        SegmentTwoIndex = segmentTwoIndex;
        Length = length;
    }

    public short SegmentOneIndex { get; init; }
    public short SegmentTwoIndex { get; init; }
    public float Length { get; init; }
}