using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Content.MapRenderer;

public sealed class MapViewerData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Tiled { get; set; } = false;
    public string Url { get; set; } = string.Empty;
    public Extent Extent { get; set; } = new();
    public string? Attributions { get; set; }
    public List<LayerGroup> LayerGroups { get; set; } = new();
}

public sealed class LayerGroup
{
    public Vector2 Scale { get; set; } = new();
    public Vector2 Offset { get; set; } = new();
    public bool Static { get; set; } = false;
    public float? MinScale { get; set; }
    public GroupSource Source { get; set; } = new();
    public List<Layer> Layers { get; set; } = new();
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
    public Vector2 ParallaxScale { get; set; } = new(0.1f, 0.1f);
}

public readonly struct Extent
{
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

    public readonly float X1;
    public readonly float Y1;
    public readonly float X2;
    public readonly float Y2;
}
