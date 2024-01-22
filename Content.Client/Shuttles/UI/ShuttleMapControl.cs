using System.Numerics;
using Content.Shared.Shuttles.Systems;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

public sealed class ShuttleMapControl : ShuttleControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private SharedShuttleSystem _shuttles;
    private SharedTransformSystem _xformSystem;

    protected override bool Draggable => true;

    public MapId ViewingMap = MapId.Nullspace;

    private Font _font;
    private Texture _backgroundTexture;

    private List<Entity<MapGridComponent>> _grids = new();

    public ShuttleMapControl() : base(128f, 2048f, 2048f)
    {
        _shuttles = _entManager.System<SharedShuttleSystem>();
        _xformSystem = _entManager.System<SharedTransformSystem>();
        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"), 10);
        _backgroundTexture = cache.GetResource<TextureResource>("/Textures/Parallaxes/KettleParallaxBG.png");
    }

    public void SetMap(MapId mapId, Vector2 offset)
    {
        ViewingMap = mapId;
        Offset = offset;
        Recentering = false;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (ViewingMap == MapId.Nullspace)
            return;

        // Draw background texture
        var tex = _backgroundTexture;
        var scale = Vector2.One * MinimapScale;
        var slowness = 1f;

        var texSize = (tex.Size.X * (int) Size.X, tex.Size.Y * (int) Size.X) * scale.Floored() / 1920;
        var ourSize = PixelSize;

        var offset = Offset;

        // Multiply offset by slowness to match normal parallax
        var scaledOffset = (offset * slowness).Floored();

        // Then modulo the scaled offset by the size to prevent drawing a bunch of offscreen tiles for really small images.
        scaledOffset.X %= texSize.X;
        scaledOffset.Y %= texSize.Y;

        // Note: scaledOffset must never be below 0 or there will be visual issues.
        // It could be allowed to be >= texSize on a given axis but that would be wasteful.

        for (var x = -scaledOffset.X; x < ourSize.X; x += texSize.X)
        {
            for (var y = -scaledOffset.Y; y < ourSize.Y; y += texSize.Y)
            {
                handle.DrawTextureRect(tex, UIBox2.FromDimensions(new Vector2(x, y), texSize));
            }
        }

        var viewBox = new Box2(Offset - WorldRangeVector, Offset + WorldRangeVector);

        _grids.Clear();
        _mapManager.FindGridsIntersecting(ViewingMap, viewBox, ref _grids, approx: true, includeMap: false);
        var matty = Matrix3.CreateInverseTransform(Offset, Angle.Zero);
        var verts = new Dictionary<Color, List<Vector2>>();
        var edges = new Dictionary<Color, List<Vector2>>();
        var strings = new Dictionary<Color, List<(Vector2, string)>>();

        // Constant size diamonds
        var diamondRadius = WorldRange / 40f;

        foreach (var grid in _grids)
        {
            var gridColor = _shuttles.GetIFFColor(grid);

            var existingVerts = verts.GetOrNew(gridColor);
            var existingEdges = edges.GetOrNew(gridColor);

            var gridPos = _xformSystem.GetWorldPosition(grid.Owner);

            var gridRelativePos = matty.Transform(gridPos);
            gridRelativePos = gridRelativePos with { Y = -gridRelativePos.Y };
            var gridUiPos = ScalePosition(gridRelativePos);

            var bottom = ScalePosition(gridRelativePos + new Vector2(0f, -2f * diamondRadius));
            var right = ScalePosition(gridRelativePos + new Vector2(diamondRadius, 0f));
            var top = ScalePosition(gridRelativePos + new Vector2(0f, 2f * diamondRadius));
            var left = ScalePosition(gridRelativePos + new Vector2(-diamondRadius, 0f));

            // Diamond interior
            existingVerts.Add(bottom);
            existingVerts.Add(right);
            existingVerts.Add(top);

            existingVerts.Add(bottom);
            existingVerts.Add(top);
            existingVerts.Add(left);

            // Diamond edges
            existingEdges.Add(bottom);
            existingEdges.Add(right);
            existingEdges.Add(right);
            existingEdges.Add(top);
            existingEdges.Add(top);
            existingEdges.Add(left);
            existingEdges.Add(left);
            existingEdges.Add(bottom);

            // Text
            var iffText = _shuttles.GetIFFLabel(grid);

            if (string.IsNullOrEmpty(iffText))
                continue;

            var existingStrings = strings.GetOrNew(gridColor);
            existingStrings.Add((gridUiPos, iffText));
        }

        // Batch the colors whoopie
        // really only affects forks with lots of grids.
        foreach (var (color, sendVerts) in verts)
        {
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, sendVerts.ToArray(), color.WithAlpha(0.05f));
        }

        foreach (var (color, sendEdges) in edges)
        {
            handle.DrawPrimitives(DrawPrimitiveTopology.LineList, sendEdges.ToArray(), color);
        }

        foreach (var (color, sendStrings) in strings)
        {
            foreach (var (gridUiPos, iffText) in sendStrings)
            {
                var textWidth = handle.GetDimensions(_font, iffText, UIScale);
                handle.DrawString(_font, gridUiPos + new Vector2(-textWidth.X / 2f, textWidth.Y), iffText, color);
            }
        }
    }
}
