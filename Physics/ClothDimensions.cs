using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace ClothDemo.Physics;

public readonly struct ClothDimensions
{
    [JsonConstructor]
    public ClothDimensions(int widthInSegments, int lengthInSegments, int segmentSize)
    {
        // If a cloth is only one segment wide or tall it becomes impossible to determine surface normals
        // and therefore render accurately without keeping track of rotation. While that is definitely
        // supportable, it's rather out of the scope of this project for now.

        ThrowIfBelowValue(widthInSegments, 2);
        ThrowIfBelowValue(lengthInSegments, 2);
        ThrowIfBelowValue(segmentSize, 1);

        WidthInSegments = widthInSegments;
        LengthInSegments = lengthInSegments;
        SegmentSize = segmentSize;

        static void ThrowIfBelowValue(int property, int minValue,
            [CallerArgumentExpression("property")] string propertyName = null)
        {
            if (property < minValue)
                throw new ArgumentOutOfRangeException(propertyName, property,
                    $"{propertyName} must be larger than or equal to {minValue}.");
        }
    }

    public int WidthInSegments { get; }
    public int LengthInSegments { get; }
    public int SegmentSize { get; }
    public int TotalSegments => WidthInSegments * LengthInSegments;

    public int TotalConstraintCount => StructuralConstraintCount + ShearConstraintCount + BendingConstraintCount;

    /// <summary>
    /// Gets the amount of constraints between horizontally and vertically adjacent segments.
    /// </summary>
    private int StructuralConstraintCount =>
        (WidthInSegments - 1) * LengthInSegments +
        WidthInSegments * (LengthInSegments - 1);

    /// <summary>
    /// Gets the amount of constraints between diagonally adjacent segments.
    /// </summary>
    private int ShearConstraintCount => (WidthInSegments - 1) * (LengthInSegments - 1) * 2;

    /// <summary>
    /// Gets the amount of constraints between segments that have one other segment in between them (horizontally, vertically or diagonally).
    /// </summary>
    private int BendingConstraintCount
    {
        get
        {
            var constraintsInRow = WidthInSegments > 1 ? WidthInSegments - 2 : 0;
            var constraintsInColumn = LengthInSegments > 1 ? LengthInSegments - 2 : 0;

            return constraintsInRow * LengthInSegments +
                   WidthInSegments * constraintsInColumn +
                   constraintsInRow * constraintsInColumn * 2;
        }
    }
}