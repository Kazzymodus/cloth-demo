using Newtonsoft.Json;

namespace ClothDemo.Effects.DataStructures;

public struct CapeWindProperties
{
    public float WindFactor { get; }
    public float FlutterSpeed { get; }
    public float FlutterStrength { get; }

    [JsonConstructor]
    public CapeWindProperties(float windFactor, float flutterSpeed, float flutterStrength)
    {
        WindFactor = windFactor;
        FlutterSpeed = flutterSpeed;
        FlutterStrength = flutterStrength;
    }
}