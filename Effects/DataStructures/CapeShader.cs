using Microsoft.Xna.Framework;

namespace ClothDemo.Effects.DataStructures;

public class CapeShader
{
    public string PassName { get; set; }
    public string Image { get; set; }
    public Vector3? Color { get; set; }
    public Vector3? SecondaryColor { get; set; }
    public float? Opacity { get; set; }
}