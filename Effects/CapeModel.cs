using System;
using ClothDemo.Effects.DataStructures;
using ClothDemo.Physics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;

namespace ClothDemo.Effects;

public class CapeModel : IDisposable
{
    private readonly Cloth _cloth;
    private readonly VertexGrid _vertexGrid;

    private readonly CapeAnchor _anchor;
    private readonly CapePhysicalProperties _physicalProperties;
    private readonly string _shaderPassName;

    private readonly Texture2D _texture;

    private bool _isDisposed;

    public CapeModel(Vector2 initialPosition, int direction, ClothDimensions dimensions, CapeAnchor anchor,
        CapePhysicalProperties physicalProperties, string shaderPassName)
    {
        _cloth = new Cloth(initialPosition, dimensions, physicalProperties.SegmentDragVariance,
            physicalProperties.StretchThreshold);

        _anchor = anchor;

        for (var i = 0; i < dimensions.WidthInSegments; i++)
            _cloth.AnchorSegment(i,
                GetSegmentAnchorPosition(initialPosition, direction,
                    dimensions.WidthInSegments * dimensions.SegmentSize,
                    i / (dimensions.WidthInSegments - 1f)));

        _vertexGrid = new VertexGrid(dimensions.WidthInSegments + 1, dimensions.LengthInSegments + 1, true);

        _physicalProperties = physicalProperties;

        var capeWidth = _cloth.Dimensions.WidthInSegments * _cloth.Dimensions.SegmentSize;
        var capeHeight = _cloth.Dimensions.LengthInSegments * _cloth.Dimensions.SegmentSize;

        _texture = new Texture2D(Main.instance.GraphicsDevice, capeWidth, capeHeight);
        var capeTextureData = new Color[capeWidth * capeHeight];
        for (var i = 0; i < capeWidth * capeHeight; i++)
            capeTextureData[i] = Color.White;
        _texture.SetData(capeTextureData);

        _shaderPassName = shaderPassName;
    }

    public void Update(Vector2 position, int direction, float simulatorDamping, int constraintPasses)
    {
        for (var i = 0; i < _cloth.Dimensions.WidthInSegments; i++)
            _cloth.AnchorSegment(i,
                GetSegmentAnchorPosition(position, direction,
                    _cloth.Dimensions.WidthInSegments * _cloth.Dimensions.SegmentSize,
                    i / (_cloth.Dimensions.WidthInSegments - 1f)));

        var simulator = new ClothSimulator(_cloth, simulatorDamping, constraintPasses);
        var force = Vector2.Zero;
        var windForce = GetWindForce();
        // Wind also lifts the cape off the ground a tad.
        force += new Vector2(windForce, -windForce);
        force.Y += _physicalProperties.GravityFactor;
        simulator.Simulate(force * (1 - _physicalProperties.Drag));

        // Because these vertices depend on segment positions there's no need to do this in Draw.
        var vertexPositions = _cloth.CalculateSegmentCorners();

        // This is a somewhat crude approximation because it relies on the assumption that the
        // cloth's origin is within the bounds of the cloth (including the corners), which is
        // a reasonable assumption but not an enforced one.
        //
        // It would be more accurate - but significantly more expensive - to check every position
        // as it's being calculated and store the lowest and highest value, but in my opinion
        // that would involve even more unwelcome coupling (two out parameters on
        // CalculateSegmentCorners()). As far as I'm concerned, this is good enough.
        var maxZ = _cloth.Dimensions.WidthInSegments * _cloth.Dimensions.SegmentSize;
        _vertexGrid.PrepareGrid(vertexPositions, VertexGridLightingMode.PerSegment, maxZ);
    }

    private float GetWindForce()
    {
        var baseWindForce = Main.windSpeedCurrent * _physicalProperties.WindProperties.WindFactor;
        var flutterFactor =
            MathF.Sin((float)Main.timeForVisualEffects * _physicalProperties.WindProperties.FlutterSpeed);
        return baseWindForce * (1f + flutterFactor * _physicalProperties.WindProperties.FlutterStrength);
    }

    private Vector3 GetSegmentAnchorPosition(Vector2 anchorPosition, int direction, int anchorLength,
        float segmentProgress)
    {
        return new Vector3(anchorPosition, 0) +
               _anchor.GetSegmentPosition(anchorLength, _cloth.Dimensions.SegmentSize, segmentProgress) *
               new Vector3(direction, 0, 1);
    }

    public void Draw(Player player, ShaderData customShader = null)
    {
        var drawData = new DrawData(
            _texture,
            // Doing this for no other reason than to support Twilight Dye, and even then it only works
            // in two of the four directions.
            new Vector2(player.position.Y, player.position.X),
            Color.White
        );

        Texture previousTexture = null;

        switch (customShader)
        {
            case ArmorShaderData armorShaderData:
                previousTexture = Main.instance.GraphicsDevice.Textures[0];
                Main.instance.GraphicsDevice.Textures[0] = _texture;
                armorShaderData.Apply(player, drawData);
                break;
            case MiscShaderData miscShaderData:
                miscShaderData.Apply(drawData);
                break;
            case null:
                GameShaders.Misc[_shaderPassName].Apply(drawData);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(customShader), customShader.GetType().Name,
                    "ShaderData type is not supported.");
        }

        _vertexGrid.Draw();

        if (previousTexture != null)
            Main.instance.GraphicsDevice.Textures[0] = previousTexture;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        if (disposing)
        {
            _texture.Dispose();
            _vertexGrid.Dispose();
        }

        _isDisposed = true;
    }

    ~CapeModel()
    {
        Dispose(false);
    }
}