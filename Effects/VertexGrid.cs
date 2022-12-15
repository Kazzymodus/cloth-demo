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

        var screenPositionVector = new Vector3(Main.screenPosition, 0);

        var colorBuffer = lightingMode == VertexGridLightingMode.PerSegment
            ? new Dictionary<Point, Color>()
            : null;

        var getColor = lightingMode switch
        {
            VertexGridLightingMode.FullBright => (Func<Vector3, Color>)((_) => Color.White),
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
            var position = positions[i];
            // This is a somewhat crude method to shrink the z-positions to the acceptable range (0 - 1)
            // without any z-fighting.
            position.Z = (position.Z + maxZ) / (maxZ * 2);
            _vertices[i] = new CustomVertexInfo(position - screenPositionVector, getColor(position),
                new Vector2(i % _width * xCoordPerStep, i / _width * yCoordPerStep));
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
            _indices.Length / 3);

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
        public Vector3 Position;
        public Color Color;
        public Vector2 TextureCoordinate;

        public static VertexDeclaration Declaration { get; } = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

        public VertexDeclaration VertexDeclaration => Declaration;

        public CustomVertexInfo(Vector3 position, Color color, Vector2 textureCoordinate)
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