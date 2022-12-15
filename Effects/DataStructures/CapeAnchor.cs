using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace ClothDemo.Effects.DataStructures;

public readonly struct CapeAnchor
{
    private readonly Vector2 _startPosition;
    private readonly Vector2 _endPosition;
    private readonly Vector3 _startTan;
    private readonly Vector3 _endTan;
    private readonly Vector3 _direction;
    private readonly float _scrunch;

    [JsonConstructor]
    public CapeAnchor(Vector2 startPosition, Vector2 endPosition, Vector3 startTan, Vector3 endTan, Vector3 direction,
        float scrunch)
    {
        _startPosition = startPosition;
        _endPosition = endPosition;
        _startTan = startTan;
        _endTan = endTan;
        _direction = direction;
        _scrunch = scrunch;
    }

    public Vector3 GetSegmentPosition(float anchorLength, float segmentSize, float progress)
    {
        return _direction * segmentSize * 0.5f +
               Vector3.Hermite(
                   new Vector3(_startPosition, 0), _startTan,
                   new Vector3(_endPosition, GetZ(anchorLength)), _endTan,
                   progress);
    }

    private float GetZ(float anchorLength)
    {
        var xyLengthSquared = (_endPosition - _startPosition).LengthSquared();
        var anchorLengthSquared = anchorLength * anchorLength;
        if (xyLengthSquared >= anchorLengthSquared)
            return 0;
        return (float)Math.Sqrt(anchorLengthSquared - xyLengthSquared) * _scrunch;
    }
}