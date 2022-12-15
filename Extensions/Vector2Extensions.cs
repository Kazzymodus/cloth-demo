using Microsoft.Xna.Framework;

namespace ClothDemo.Extensions;

internal static class Vector3Extensions
{
    internal static Vector2 XY(this Vector3 vector3)
    {
        return new Vector2(vector3.X, vector3.Y);
    }
}