using System;
using Microsoft.Xna.Framework;

namespace ClothDemo.Physics;

public class Cloth
{
    private readonly ClothDimensions _dimensions;

    public Cloth(Vector2 initialPosition, ClothDimensions dimensions, float segmentDragVariance, float stretchThreshold)
    {
        SegmentDragVariance = segmentDragVariance;
        StretchThreshold = stretchThreshold;
        _dimensions = dimensions;
        Segments = new ClothSegment[dimensions.TotalSegments];
        Constraints = new ClothConstraint[dimensions.TotalConstraintCount];

        CreateSegments(initialPosition, dimensions.SegmentSize);
    }

    // I use the field internally to save on struct copying.
    public ClothDimensions Dimensions => _dimensions;
    public float SegmentDragVariance { get; }
    public float StretchThreshold { get; }

    public ClothSegment[] Segments { get; }
    public ClothConstraint[] Constraints { get; }

    public void AnchorSegment(int segmentIndex, Vector3? position)
    {
        if (segmentIndex < 0 || segmentIndex >= Segments.Length)
            throw new ArgumentOutOfRangeException(nameof(segmentIndex), segmentIndex,
                $"{nameof(segmentIndex)} is out of bounds of the array");

        Segments[segmentIndex].IsAnchored = true;
        if (position.HasValue)
            Segments[segmentIndex].Position = position.Value;
    }

    // There is admittedly some sleight-of-hand going on here as the only real purpose of this
    // method is to calculate vertex positions, which implies a tight coupling between the
    // physical representation of the cloth and its rendering. However, this is still the most
    // appropriate place for it, if only because corner positions are still a physical property
    // of the cloth, and can theoretically be used elsewhere.
    public Vector3[] CalculateSegmentCorners()
    {
        var widthInVertices = _dimensions.WidthInSegments + 1;
        var lengthInVertices = _dimensions.LengthInSegments + 1;
        var positions = new Vector3[widthInVertices * lengthInVertices];

        // In order to calculate the vertex representation of the cloth, we first deal with the easy ones:
        // those that are a corner of four different segments, because the position is simply the average
        // of the position of those segments. These are all the vertices that are not on the outer edges of
        // the cloth.

        var innerVerticesWidth = _dimensions.WidthInSegments - 1;
        var innerVerticesLength = _dimensions.LengthInSegments - 1;
        var innerVerticesCount = innerVerticesWidth * innerVerticesLength;
        var firstInnerVertexIndex = _dimensions.WidthInSegments + 2;

        for (var i = 0; i < innerVerticesCount; i++)
        {
            var vertexIndex = firstInnerVertexIndex +
                              i / innerVerticesWidth * widthInVertices +
                              i % innerVerticesWidth;
            var upperLeftSegmentIndex = i % innerVerticesWidth +
                                        i / innerVerticesWidth * _dimensions.WidthInSegments;
            positions[vertexIndex] = CalculateInnerVertex(upperLeftSegmentIndex);
        }

        // Then we calculate the vertices of the top and bottom rows (excluding the corners). We can
        // calculate the (relative) X position quite easily by taking the average of two adjacent
        // segments. We then use that position to point reflect the corresponding inner vertex, putting
        // it in the correct spot.

        var lastRowVertexOffset = widthInVertices * _dimensions.LengthInSegments;
        var lastRowSegmentOffset = _dimensions.WidthInSegments * (_dimensions.LengthInSegments - 1);

        for (var i = 1; i < _dimensions.WidthInSegments; i++)
        {
            var topRowSegmentAverage = (Segments[i - 1].Position + Segments[i].Position) * 0.5f;
            positions[i] = PointReflect(positions[i + _dimensions.WidthInSegments + 1], topRowSegmentAverage);

            var bottomRowSegmentAverage = (Segments[i + lastRowSegmentOffset - 1].Position +
                                           Segments[i + lastRowSegmentOffset].Position) * 0.5f;
            positions[i + lastRowVertexOffset] =
                PointReflect(positions[i + lastRowVertexOffset - 1 - _dimensions.WidthInSegments],
                    bottomRowSegmentAverage);
        }

        // Then we move on to the leftmost and rightmost columns, using the same method as above.

        for (var i = 1; i < _dimensions.LengthInSegments; i++)
        {
            var vertexIndex = i * widthInVertices;
            var leftColumnSegmentAverage =
                (Segments[(i - 1) * _dimensions.WidthInSegments].Position +
                 Segments[i * _dimensions.WidthInSegments].Position) * 0.5f;
            positions[vertexIndex] =
                PointReflect(positions[vertexIndex + 1], leftColumnSegmentAverage);

            vertexIndex += _dimensions.WidthInSegments;
            var rightColumnSegmentAverage =
                (Segments[i * _dimensions.WidthInSegments - 1].Position +
                 Segments[i * _dimensions.WidthInSegments + _dimensions.WidthInSegments - 1].Position) * 0.5f;
            positions[vertexIndex] =
                PointReflect(positions[vertexIndex - 1], rightColumnSegmentAverage);
        }

        // Finally, we sort out the corners, which are also mirrored from their opposing vertex.

        positions[0] = PointReflect(positions[_dimensions.WidthInSegments + 2], Segments[0].Position);

        positions[_dimensions.WidthInSegments] = PointReflect(positions[_dimensions.WidthInSegments * 2],
            Segments[_dimensions.WidthInSegments - 1].Position);

        positions[lastRowVertexOffset] = PointReflect(
            positions[widthInVertices * (_dimensions.LengthInSegments - 1) + 1],
            Segments[lastRowSegmentOffset].Position);

        positions[^1] = PointReflect(positions[widthInVertices * _dimensions.LengthInSegments - 2],
            Segments[^1].Position);

        return positions;

        Vector3 CalculateInnerVertex(int upperLeftSegmentIndex)
        {
            return 0.25f *
                   (Segments[upperLeftSegmentIndex].Position +
                    Segments[upperLeftSegmentIndex + 1].Position +
                    Segments[upperLeftSegmentIndex + _dimensions.WidthInSegments].Position +
                    Segments[upperLeftSegmentIndex + _dimensions.WidthInSegments + 1].Position);
        }

        // Can be rewritten to 2x - y, but this is more intuitive.

        Vector3 PointReflect(Vector3 position, Vector3 reflectionPoint)
        {
            return reflectionPoint - (position - reflectionPoint);
        }
    }

    private void CreateSegments(Vector2 initialPosition, float segmentSize)
    {
        var constraintIndex = 0;

        for (short y = 0; y < _dimensions.LengthInSegments; y++)
        for (short x = 0; x < _dimensions.WidthInSegments; x++)
        {
            var segmentIndex = y * _dimensions.WidthInSegments + x;
            var position = new Vector3(initialPosition, 0);
            position.Y += segmentSize;

            Segments[segmentIndex] = new ClothSegment(position);

            CreateConstraintsForSegment((short)segmentIndex, ref constraintIndex, segmentSize);
        }
    }

    private void CreateConstraintsForSegment(short segmentIndex, ref int nextConstraintIndex,
        float baseConstraintLength)
    {
        // diagonalSegmentLength is constant for an entire row, so theoretically I could've done the calculation there
        // and then pass it to this method as an argument, but I felt that unnecessarily padded the signature. Using
        // a pre-computed value of Sqrt(2), I felt this is the most elegant solution, and the performance overhead
        // of doing this calculation for every segment rather than every row is negligible in its own right, let
        // alone in a constructor that generally isn't called more than once.

        var diagonalConstraintLength = Utils.MathHelper.Sqrt2 * baseConstraintLength;

        var (segmentsToTheLeft, segmentsToTheRight) = CountLateralSegments();
        var segmentsBeneath = CountSegmentsBeneath();

        if (segmentsToTheRight >= 1)
        {
            CreateNextConstraint(
                ref nextConstraintIndex,
                segmentIndex,
                (short)(segmentIndex + 1),
                baseConstraintLength
            );

            if (segmentsBeneath >= 1)
                CreateNextConstraint(
                    ref nextConstraintIndex,
                    segmentIndex,
                    (short)(segmentIndex + _dimensions.WidthInSegments + 1),
                    diagonalConstraintLength
                );

            if (segmentsToTheRight >= 2)
            {
                CreateNextConstraint(
                    ref nextConstraintIndex,
                    segmentIndex,
                    (short)(segmentIndex + 2),
                    baseConstraintLength * 2
                );

                if (segmentsBeneath >= 2)
                    CreateNextConstraint(
                        ref nextConstraintIndex,
                        segmentIndex,
                        (short)(segmentIndex + _dimensions.WidthInSegments * 2 + 2),
                        diagonalConstraintLength * 2
                    );
            }
        }

        if (segmentsBeneath >= 1)
        {
            CreateNextConstraint(
                ref nextConstraintIndex,
                segmentIndex,
                (short)(segmentIndex + _dimensions.WidthInSegments),
                baseConstraintLength
            );

            if (segmentsToTheLeft >= 1)
                CreateNextConstraint(ref nextConstraintIndex,
                    segmentIndex,
                    (short)(segmentIndex + _dimensions.WidthInSegments - 1),
                    diagonalConstraintLength
                );

            if (segmentsBeneath >= 2)
            {
                CreateNextConstraint(
                    ref nextConstraintIndex,
                    segmentIndex,
                    (short)(segmentIndex + _dimensions.WidthInSegments * 2),
                    baseConstraintLength * 2
                );

                if (segmentsToTheLeft >= 2)
                    CreateNextConstraint(
                        ref nextConstraintIndex,
                        segmentIndex,
                        (short)(segmentIndex + _dimensions.WidthInSegments * 2 - 2),
                        diagonalConstraintLength * 2
                    );
            }
        }

        (int segmentsToTheLeft, int segmentsToTheRight) CountLateralSegments()
        {
            var left = segmentIndex % _dimensions.WidthInSegments;
            var right = _dimensions.WidthInSegments - (left + 1);
            return (left, right);
        }

        int CountSegmentsBeneath()
        {
            return _dimensions.LengthInSegments - (segmentIndex / _dimensions.WidthInSegments + 1);
        }
    }

    private void CreateNextConstraint(ref int nextConstraintIndex, short segmentIndex1, short segmentIndex2,
        float constraintLength)
    {
        Constraints[nextConstraintIndex++] =
            new ClothConstraint(segmentIndex1, segmentIndex2, constraintLength);
    }
}