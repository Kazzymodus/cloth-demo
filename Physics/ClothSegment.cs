using Microsoft.Xna.Framework;

namespace ClothDemo.Physics;

public struct ClothSegment
{
    public ClothSegment(Vector3 position, bool isAnchored = false)
    {
        OldPosition = Position = position;
        IsAnchored = isAnchored;
    }

    public Vector3 Position { get; set; }
    public Vector3 OldPosition { get; set; }
    public bool IsAnchored { get; set; }
}