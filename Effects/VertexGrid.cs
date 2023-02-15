using System;
using System.Collections.Generic;
using ClothDemo.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ClothDemo.Effects;

// Effectively this is a two-dimensional interpretation of VertexStrip (which is why it's so similar).

public class VertexGrid : IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly bool _includeBacksides;
    private readonly CustomVertexInfo[] _vertices;
    private readonly short[] _indices;

    private const int FacesPerQuad = 2;
    private const int VerticesPerFace = 3;

    private readonly VertexBuffer _vertexBuffer;
    private readonly IndexBuffer _indexBuffer;

    private bool _isDisposed;

    public VertexGrid(int width, int height, bool includeBacksides = false)
    {
        if (width < 1)
            throw new ArgumentOutOfRangeException(nameof(width), width, $"{nameof(width)} must be larger than 0");
        if (height < 1)
            throw new ArgumentOutOfRangeException(nameof(height), height, $"{nameof(height)} must be larger than 0");

        _width = width;
        _height = height;
        _includeBacksides = includeBacksides;

        var vertexCount = width * height;
        _vertices = new CustomVertexInfo[vertexCount];
        _vertexBuffer = new VertexBuffer(Main.instance.GraphicsDevice, CustomVertexInfo.Declaration, width * height,
            BufferUsage.WriteOnly);

        var verticesPerSegment = FacesPerQuad * VerticesPerFace;
        if (includeBacksides)
            verticesPerSegment *= 2;

        var indexCount = (width - 1) * (height - 1) * verticesPerSegment;
        _indices = new short[indexCount];
        _indexBuffer = new IndexBuffer(Main.instance.GraphicsDevice, IndexElementSize.SixteenBits, indexCount,
            BufferUsage.WriteOnly);

        ConfigureIndices();
        _indexBuffer.SetData(_indices);
    }

    public void PrepareGrid(Vector3[] positions, VertexGridLightingMode lightingMode, int maxZ)
    {
        if (positions.Length != _width * _height)
            throw new ArgumentOutOfRangeException(nameof(positions), positions.Length,
                $"Expected {_width * _height} positions.");

        var xCoordPerStep = 1f / (_width - 1);
        var yCoordPerStep = 1f / (_height - 1);

        var screenPositionVector = new Vector4(Main.screenPosition, 0, 0);

        var colorBuffer = lightingMode == VertexGridLightingMode.PerSegment
            ? new Dictionary<Point, Color>()
            : null;

        var getColor = lightingMode switch
        {
            VertexGridLightingMode.FullBright => (Func<Vector4, Color>)((_) => Color.White),
            VertexGridLightingMode.PerSegment => (position) =>
            {
                var tile = position.XY().ToTileCoordinates();
                if (colorBuffer!.TryGetValue(tile, out var color))
                    return color;

                var tileColor = Lighting.GetColor(tile);
                colorBuffer.Add(tile, tileColor);
                return tileColor;
            },
            _ => throw new ArgumentOutOfRangeException($"Lighting mode {lightingMode} not supported.")
        };

        for (var i = 0; i < positions.Length; i++)
        {
            var position = new Vector4(positions[i], 1);
            // This is a somewhat crude method to shrink the z-positions to the acceptable range (0 - 1)
            // without any z-fighting.
            position.Z = (position.Z + maxZ) / (maxZ * 2);
            _vertices[i] = new CustomVertexInfo(position - screenPositionVector, getColor(position),
                new Vector3(
                    i % _width * xCoordPerStep,
                    i / _width * yCoordPerStep,
                    1));
        }

        for (var i = 0; i < (_width - 1) * (_height - 1); i++)
        {
            var vertexOne = _vertices[i].Position.XY();
            var vertexTwo = _vertices[i + 1].Position.XY();
            ;
            var vertexThree = _vertices[i + _width].Position.XY();
            ;
            var vertexFour = _vertices[i + _width + 1].Position.XY();
            ;

            var diagonalOne = vertexFour - vertexOne;
            var diagonalTwo = vertexThree - vertexTwo;
            var a1 = diagonalOne.Y / diagonalOne.X;
            var a2 = diagonalTwo.Y / diagonalTwo.X;

            var b1 = vertexOne.Y;
            var b2 = vertexThree.Y;
            var x = (-b2 + b1) / (a2 - a1);
            var y = b1 + a1 * x;
            var intersection = new Vector2(x + vertexOne.X, y);

            var d1 = (intersection - vertexOne).Length();
            var d2 = (intersection - vertexTwo).Length();
            var d3 = (intersection - vertexThree).Length();
            var d4 = (intersection - vertexFour).Length();

            var coords1 = _vertices[i].TextureCoordinate * ((d1 + d4) / d4);
            var coords2 = _vertices[i + 1].TextureCoordinate * ((d2 + d3) / d3);
            var coords3 = _vertices[i + _width].TextureCoordinate * ((d3 + d2) / d2);
            var coords4 = _vertices[i + _width + 1].TextureCoordinate * ((d4 + d1) / d1);

            _vertices[i].TextureCoordinate = coords1 / coords1.Z;
            _vertices[i + 1].TextureCoordinate = coords2 / coords2.Z;
            _vertices[i + _width].TextureCoordinate = coords3 / coords3.Z;
            _vertices[i + _width + 1].TextureCoordinate = coords4 / coords4.Z;
        }

        _vertexBuffer.SetData(_vertices);
    }

    public void Draw()
    {
        var previousBuffers = Main.instance.GraphicsDevice.GetVertexBuffers();
        var previousIndices = Main.instance.GraphicsDevice.Indices;

        Main.instance.GraphicsDevice.SetVertexBuffers(_vertexBuffer);
        Main.instance.GraphicsDevice.Indices = _indexBuffer;

        Main.instance.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertices.Length, 0,
            _indices.Length / VerticesPerFace);

        Main.instance.GraphicsDevice.SetVertexBuffers(previousBuffers);
        Main.instance.GraphicsDevice.Indices = previousIndices;
    }

    // Unlike VertexStrip, indices are the same throughout the lifetime of the grid, so we only have
    // to do this once.

    private void ConfigureIndices()
    {
        var widthInQuads = _width - 1;

        for (var y = 0; y < _height - 1; y++)
        for (short x = 0; x < widthInQuads; x++)
        {
            var indexIndex = (y * widthInQuads + x) * FacesPerQuad * VerticesPerFace;
            if (_includeBacksides)
                indexIndex *= 2;

            var firstVertexIndex = (short)(y * _width + x);
            var secondVertexIndex = (short)(firstVertexIndex + 1);
            var thirdVertexIndex = (short)(firstVertexIndex + _width);
            var fourthVertexIndex = (short)(thirdVertexIndex + 1);

            _indices[indexIndex] = firstVertexIndex;
            _indices[++indexIndex] = secondVertexIndex;
            _indices[++indexIndex] = thirdVertexIndex;

            _indices[++indexIndex] = thirdVertexIndex;
            _indices[++indexIndex] = secondVertexIndex;
            _indices[++indexIndex] = fourthVertexIndex;

            if (!_includeBacksides)
                continue;

            _indices[++indexIndex] = firstVertexIndex;
            _indices[++indexIndex] = thirdVertexIndex;
            _indices[++indexIndex] = secondVertexIndex;

            _indices[++indexIndex] = thirdVertexIndex;
            _indices[++indexIndex] = fourthVertexIndex;
            _indices[++indexIndex] = secondVertexIndex;
        }
    }

    private struct CustomVertexInfo : IVertexType
    {
        public Vector4 Position;
        public Color Color;
        public Vector3 TextureCoordinate;

        public static VertexDeclaration Declaration { get; } = new(
            new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),
            new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(20, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0));

        public VertexDeclaration VertexDeclaration => Declaration;

        public CustomVertexInfo(Vector4 position, Color color, Vector3 textureCoordinate)
        {
            Position = position;
            Color = color;
            TextureCoordinate = textureCoordinate;
        }
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
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
        }

        _isDisposed = true;
    }

    ~VertexGrid()
    {
        Dispose(false);
    }
}