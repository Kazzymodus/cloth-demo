using System;
using ClothDemo.Extensions;
using Microsoft.Xna.Framework;
using Terraria;

namespace ClothDemo.Physics;

public class ClothSimulator
{
    private readonly Cloth _cloth;
    private readonly float _damping;
    private readonly int _constraintPasses;

    public ClothSimulator(Cloth cloth, float damping, int constraintPasses)
    {
        _cloth = cloth;
        _damping = damping;
        _constraintPasses = constraintPasses;
    }

    public void Simulate(Vector2 force)
    {
        ApplyForce(force);

        // Enforcing the constraint multiple times prevents weird artifacts, especially when moving too fast.
        // Of course, this also means more processing time.

        for (var i = 0; i < _constraintPasses; i++)
            EnforceConstraints();
    }

    private void EnforceConstraints()
    {
        foreach (var constraint in _cloth.Constraints)
            EnforceConstraints(
                ref _cloth.Segments[constraint.SegmentOneIndex],
                ref _cloth.Segments[constraint.SegmentTwoIndex],
                constraint.Length
            );
    }

    // Constraints are applied in ClothSimulator rather than ClothConstraint because ClothConstraint
    // does not contain references to segments but merely their index in the Segments array.
    // That means I would have to pass the Segments array to the ClothConstraints class in order to
    // apply them, which I feel would be a rather ugly coupling.
    //
    // The reason I'm using indices rather than references in the first place
    // is to maintain data locality. Because this is a relatively expensive algorithm
    // for a purely visual effect, I feel that is a worthwhile optimisation.

    private void EnforceConstraints(ref ClothSegment segment1, ref ClothSegment segment2, float constraintLength)
    {
        var segmentVector = segment2.Position - segment1.Position;
        var constraintRatio = constraintLength / segmentVector.Length();
        if (float.IsInfinity(constraintRatio))
            constraintRatio = 0;
        var correctionVector =
            segmentVector * (1 - constraintRatio) * 0.5f;

        ConstrainSegment(ref segment1, correctionVector);
        ConstrainSegment(ref segment2, -correctionVector);

        void ConstrainSegment(ref ClothSegment segment, Vector3 direction)
        {
            if (segment.IsAnchored)
                return;

            if (constraintRatio < _cloth.StretchThreshold || !IsBlocked(segment.Position, direction))
                segment.Position += direction;
        }
    }

    private static bool IsBlocked(Vector3 segmentPosition, Vector3 direction)
    {
        return Collision.IsWorldPointSolid((segmentPosition + direction).XY());
    }

    private void ApplyForce(Vector2 force)
    {
        // Not using a foreach because then I can't use ref.

        for (var i = 0; i < _cloth.Segments.Length; i++)
        {
            if (_cloth.Segments[i].IsAnchored)
                continue;

            var specificForce = force;
            if (_cloth.SegmentDragVariance > 0)
                specificForce *= new Vector2(GetRandomSegmentDrag(), GetRandomSegmentDrag());

            ApplyForceToSegment(ref _cloth.Segments[i], specificForce);
        }

        float GetRandomSegmentDrag()
        {
            return 1f + Main.rand.NextFloat(-_cloth.SegmentDragVariance, _cloth.SegmentDragVariance);
        }
    }

    private void ApplyForceToSegment(ref ClothSegment segment, Vector2 force)
    {
        var currentPosition = segment.Position;
        var acceleration = new Vector3(force, Math.Abs(force.X));
        var newPosition = segment.Position + (currentPosition - segment.OldPosition) * (1 - _damping) + acceleration;
        if (Collision.IsWorldPointSolid(newPosition.XY()))
            return;
        segment.Position = newPosition;
        segment.OldPosition = currentPosition;
    }
}