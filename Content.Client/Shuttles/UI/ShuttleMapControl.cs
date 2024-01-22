using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Client.Shuttles.UI;

public sealed class ShuttleMapControl : ShuttleControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private SharedTransformSystem _xformSystem;

    protected override bool Draggable => true;

    public MapId ViewingMap = MapId.Nullspace;

    private Font _font;

    private List<Entity<MapGridComponent>> _grids = new();

    public ShuttleMapControl() : base(128f, 2048f, 2048f)
    {
        _xformSystem = _entManager.System<SharedTransformSystem>();
        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf"), 10);
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

        var viewBox = new Box2(Offset - WorldRangeVector, Offset + WorldRangeVector);

        _grids.Clear();
        _mapManager.FindGridsIntersecting(ViewingMap, viewBox, ref _grids, approx: true, includeMap: false);
        var matty = Matrix3.CreateInverseTransform(Offset, Angle.Zero);
        var verts = new ValueList<Vector2>(_grids.Count * 6);
        var edges = new ValueList<Vector2>(_grids.Count * 8);
        var strings = new List<string>(_grids.Count);

        // Constant size diamonds
        var diamondRadius = WorldRange / 40f;

        foreach (var grid in _grids)
        {
            var gridPos = _xformSystem.GetWorldPosition(grid.Owner);

            var gridRelativePos = matty.Transform(gridPos);
            gridRelativePos = gridRelativePos with { Y = -gridRelativePos.Y };
            var gridUiPos = ScalePosition(gridRelativePos);

            var bottom = ScalePosition(gridRelativePos + new Vector2(0f, -2f * diamondRadius));
            var right = ScalePosition(gridRelativePos + new Vector2(diamondRadius, 0f));
            var top = ScalePosition(gridRelativePos + new Vector2(0f, 2f * diamondRadius));
            var left = ScalePosition(gridRelativePos + new Vector2(-diamondRadius, 0f));

            // Diamond interior
            verts.Add(bottom);
            verts.Add(right);
            verts.Add(top);

            verts.Add(bottom);
            verts.Add(top);
            verts.Add(left);

            // Diamond edges
            edges.Add(bottom);
            edges.Add(right);
            edges.Add(right);
            edges.Add(top);
            edges.Add(top);
            edges.Add(left);
            edges.Add(left);
            edges.Add(bottom);

            // Text
            var iffText = _entManager.GetComponent<MetaDataComponent>(grid.Owner).EntityName;

            var textWidth = handle.GetDimensions(_font, iffText, UIScale);
            handle.DrawString(_font, gridUiPos + new Vector2(-textWidth.X / 2f, textWidth.Y), iffText, Color.Lime);
        }

        var drawColor = Color.Lime;

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, verts.Span, drawColor.WithAlpha(0.05f));
        handle.DrawPrimitives(DrawPrimitiveTopology.LineList, edges.Span, drawColor);
    }
}
