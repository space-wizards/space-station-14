using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.Maths;
using SixLabors.ImageSharp.PixelFormats;

namespace Content.MapRenderer;

public sealed class MapViewerData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<GridLayer> Grids { get; set; } = new();
    public string? Attributions { get; set; }
    public List<LayerGroup> ParallaxLayers { get; set; } = new();
}

public sealed class GridLayer
{
    public string GridId { get; set; } = string.Empty;
    public Position Offset { get; set; }
    public bool Tiled { get; set; } = false;
    public string Url { get; set; }
    public Extent Extent { get; set; }

    public GridLayer(RenderedGridImage<Rgba32> gridImage, string url)
    {
        //Get the internal _uid as string
        if (gridImage.GridUid.HasValue)
            GridId = gridImage.GridUid.Value.GetHashCode().ToString();

        Offset = new Position(gridImage.Offset);
        Extent = new Extent(gridImage.Image.Width, gridImage.Image.Height);
        Url = url;
    }
}

public sealed class LayerGroup
{
    public Position Scale { get; set; } = Position.One();
    public Position Offset { get; set; } = Position.Zero();
    public bool Static { get; set; } = false;
    public float? MinScale { get; set; }
    public GroupSource Source { get; set; } = new();
    public List<Layer> Layers { get; set; } = new();

    public static LayerGroup DefaultParallax()
    {
        return new LayerGroup
        {
            Scale = new Position(0.1f, 0.1f),
            Source = new GroupSource
            {
                Url = "https://i.imgur.com/3YO8KRd.png",
                Extent = new Extent(6000, 4000)
            },
            Layers = new List<Layer>
            {
                new()
                {
                    Url = "https://i.imgur.com/IannmmK.png"
                },
                new()
                {
                    Url = "https://i.imgur.com/T3W6JsE.png",
                    Composition = "lighter",
                    ParallaxScale = new Position(0.2f, 0.2f)
                },
                new()
                {
                    Url = "https://i.imgur.com/T3W6JsE.png",
                    Composition = "lighter",
                    ParallaxScale = new Position(0.3f, 0.3f)
                }
            }
        };
    }
}

public sealed class GroupSource
{
    public string Url { get; set; } = string.Empty;
    public Extent Extent { get; set; } = new();
}

public sealed class Layer
{
    public string Url { get; set; } = string.Empty;
    public string Composition { get; set; } = "source-over";
    public Position ParallaxScale { get; set; } = new(0.1f, 0.1f);
}

public readonly struct Extent
{
    public readonly float X1;
    public readonly float Y1;
    public readonly float X2;
    public readonly float Y2;

    public Extent()
    {
        X1 = 0;
        Y1 = 0;
        X2 = 0;
        Y2 = 0;
    }

    public Extent(float x2, float y2)
    {
        X1 = 0;
        Y1 = 0;
        X2 = x2;
        Y2 = y2;
    }

    public Extent(float x1, float y1, float x2, float y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }
}

public readonly struct Position
{
    public readonly float X;
    public readonly float Y;

    public Position(float x, float y)
    {
        X = x;
        Y = y;
    }

    public Position(Vector2 vector2)
    {
        X = vector2.X;
        Y = vector2.Y;
    }

    public static Position Zero()
    {
        return new Position(0, 0);
    }

    public static Position One()
    {
        return new Position(0, 0);
    }
}
