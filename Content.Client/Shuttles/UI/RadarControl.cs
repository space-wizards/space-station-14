using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Collections;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Displays nearby grids inside of a control.
/// </summary>
public sealed class RadarControl : ShuttleControl
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    private SharedMapSystem _maps;
    private SharedTransformSystem _transform;

    private const float GridLinesDistance = 32f;

    /// <summary>
    /// Used to transform all of the radar objects. Typically is a shuttle console parented to a grid.
    /// </summary>
    private EntityCoordinates? _coordinates;

    private Angle? _rotation;

    /// <summary>
    /// Shows a label on each radar object.
    /// </summary>
    private Dictionary<EntityUid, Control> _iffControls = new();

    private Dictionary<EntityUid, List<DockingInterfaceState>> _docks = new();

    public bool ShowIFF { get; set; } = true;
    public bool ShowDocks { get; set; } = true;

    /// <summary>
    /// Currently hovered docked to show on the map.
    /// </summary>
    public NetEntity? HighlightedDock;

    /// <summary>
    /// Raised if the user left-clicks on the radar control with the relevant entitycoordinates.
    /// </summary>
    public Action<EntityCoordinates>? OnRadarClick;

    private List<Entity<MapGridComponent>> _grids = new();

    public RadarControl() : base(64f, 256f, 256f)
    {
        _maps = _entManager.System<SharedMapSystem>();
        _transform = _entManager.System<SharedTransformSystem>();
    }

    public void SetMatrix(EntityCoordinates? coordinates, Angle? angle)
    {
        _coordinates = coordinates;
        _rotation = angle;
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        base.KeyBindUp(args);

        if (_coordinates == null || _rotation == null || args.Function != EngineKeyFunctions.UIClick ||
            OnRadarClick == null)
        {
            return;
        }

        var a = InverseScalePosition(args.RelativePosition);
        var relativeWorldPos = new Vector2(a.X, -a.Y);
        relativeWorldPos = _rotation.Value.RotateVec(relativeWorldPos);
        var coords = _coordinates.Value.Offset(relativeWorldPos);
        OnRadarClick?.Invoke(coords);
    }

    /// <summary>
    /// Gets the entitycoordinates of where the mouseposition is, relative to the control.
    /// </summary>
    [PublicAPI]
    public EntityCoordinates GetMouseCoordinates(ScreenCoordinates screen)
    {
        if (_coordinates == null || _rotation == null)
        {
            return EntityCoordinates.Invalid;
        }

        var pos = screen.Position / UIScale - GlobalPosition;

        var a = InverseScalePosition(pos);
        var relativeWorldPos = new Vector2(a.X, -a.Y);
        relativeWorldPos = _rotation.Value.RotateVec(relativeWorldPos);
        var coords = _coordinates.Value.Offset(relativeWorldPos);
        return coords;
    }

    public void UpdateState(RadarConsoleBoundInterfaceState ls)
    {
        WorldMaxRange = ls.MaxRange;

        if (WorldMaxRange < WorldRange)
        {
            ActualRadarRange = WorldMaxRange;
        }

        if (WorldMaxRange < WorldMinRange)
            WorldMinRange = WorldMaxRange;

        ActualRadarRange = Math.Clamp(ActualRadarRange, WorldMinRange, WorldMaxRange);

        _docks.Clear();

        foreach (var state in ls.Docks)
        {
            var coordinates = state.Coordinates;
            var grid = _docks.GetOrNew(_entManager.GetEntity(coordinates.NetEntity));
            grid.Add(state);
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        // No data
        if (_coordinates == null || _rotation == null)
        {
            Clear();
            return;
        }

        var gridLines = new Color(0.08f, 0.08f, 0.08f);
        var gridLinesRadial = 8;
        var gridLinesEquatorial = (int) Math.Floor(WorldRange / GridLinesDistance);

        for (var i = 1; i < gridLinesEquatorial + 1; i++)
        {
            handle.DrawCircle(new Vector2(MidPoint, MidPoint), GridLinesDistance * MinimapScale * i, gridLines, false);
        }

        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            var aExtent = angle.ToVec() * ScaledMinimapRadius;
            handle.DrawLine(new Vector2(MidPoint, MidPoint) - aExtent, new Vector2(MidPoint, MidPoint) + aExtent, gridLines);
        }

        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var fixturesQuery = _entManager.GetEntityQuery<FixturesComponent>();
        var bodyQuery = _entManager.GetEntityQuery<PhysicsComponent>();

        if (!xformQuery.TryGetComponent(_coordinates.Value.EntityId, out var xform)
            || xform.MapID == MapId.Nullspace)
        {
            Clear();
            return;
        }

        var (pos, rot) = _transform.GetWorldPositionRotation(xform);
        var offset = _coordinates.Value.Position;
        var offsetMatrix = Matrix3.CreateInverseTransform(pos, rot + _rotation.Value);

        // Draw our grid in detail
        var ourGridId = xform.GridUid;
        if (_entManager.TryGetComponent<MapGridComponent>(ourGridId, out var ourGrid) &&
            fixturesQuery.HasComponent(ourGridId.Value))
        {
            var ourGridMatrix = _transform.GetWorldMatrix(ourGridId.Value);
            Matrix3.Multiply(in ourGridMatrix, in offsetMatrix, out var matrix);

            DrawGrid(handle, matrix, (ourGridId.Value, ourGrid), Color.MediumSpringGreen, true);
            DrawDocks(handle, ourGridId.Value, matrix);
        }

        var invertedPosition = _coordinates.Value.Position - offset;
        invertedPosition.Y = -invertedPosition.Y;
        // Don't need to transform the InvWorldMatrix again as it's already offset to its position.

        // Draw radar position on the station
        handle.DrawCircle(ScalePosition(invertedPosition), 5f, Color.Lime);

        var shown = new HashSet<EntityUid>();

        _grids.Clear();
        _mapManager.FindGridsIntersecting(xform.MapID, new Box2(pos - MaxRadarRangeVector, pos + MaxRadarRangeVector), ref _grids, approx: true, includeMap: false);

        // Draw other grids... differently
        foreach (var grid in _grids)
        {
            var gUid = grid.Owner;
            if (gUid == ourGridId || !fixturesQuery.HasComponent(gUid))
                continue;

            var gridBody = bodyQuery.GetComponent(gUid);
            if (gridBody.Mass < 10f)
            {
                ClearLabel(gUid);
                continue;
            }

            _entManager.TryGetComponent<IFFComponent>(gUid, out var iff);

            // Hide it entirely.
            if (iff != null &&
                (iff.Flags & IFFFlags.Hide) != 0x0)
            {
                continue;
            }

            shown.Add(gUid);
            var name = metaQuery.GetComponent(gUid).EntityName;

            if (name == string.Empty)
                name = Loc.GetString("shuttle-console-unknown");

            var gridMatrix = _transform.GetWorldMatrix(gUid);
            Matrix3.Multiply(in gridMatrix, in offsetMatrix, out var matty);
            var color = iff?.Color ?? Color.Gold;

            // Others default:
            // Color.FromHex("#FFC000FF")
            // Hostile default: Color.Firebrick

            if (ShowIFF &&
                (iff == null && IFFComponent.ShowIFFDefault ||
                 (iff.Flags & IFFFlags.HideLabel) == 0x0))
            {
                var gridBounds = grid.Comp.LocalAABB;
                Label label;

                if (!_iffControls.TryGetValue(gUid, out var control))
                {
                    label = new Label()
                    {
                        HorizontalAlignment = HAlignment.Left,
                    };

                    _iffControls[gUid] = label;
                    AddChild(label);
                }
                else
                {
                    label = (Label) control;
                }

                label.FontColorOverride = color;
                var gridCentre = matty.Transform(gridBody.LocalCenter);
                gridCentre.Y = -gridCentre.Y;
                var distance = gridCentre.Length();

                // y-offset the control to always render below the grid (vertically)
                var yOffset = Math.Max(gridBounds.Height, gridBounds.Width) * MinimapScale / 1.8f / UIScale;

                // The actual position in the UI. We offset the matrix position to render it off by half its width
                // plus by the offset.
                var uiPosition = ScalePosition(gridCentre) / UIScale - new Vector2(label.Width / 2f, -yOffset);

                // Look this is uggo so feel free to cleanup. We just need to clamp the UI position to within the viewport.
                uiPosition = new Vector2(Math.Clamp(uiPosition.X, 0f, Width - label.Width),
                    Math.Clamp(uiPosition.Y, 10f, Height - label.Height));

                label.Visible = true;
                label.Text = Loc.GetString("shuttle-console-iff-label", ("name", name), ("distance", $"{distance:0.0}"));
                LayoutContainer.SetPosition(label, uiPosition);
            }
            else
            {
                ClearLabel(gUid);
            }

            // Detailed view
            DrawGrid(handle, matty, grid, color, true);

            DrawDocks(handle, gUid, matty);
        }

        foreach (var (ent, _) in _iffControls)
        {
            if (shown.Contains(ent)) continue;
            ClearLabel(ent);
        }
    }

    private void Clear()
    {
        foreach (var (_, label) in _iffControls)
        {
            label.Dispose();
        }

        _iffControls.Clear();
    }

    private void ClearLabel(EntityUid uid)
    {
        if (!_iffControls.TryGetValue(uid, out var label)) return;
        label.Dispose();
        _iffControls.Remove(uid);
    }

    private void DrawDocks(DrawingHandleScreen handle, EntityUid uid, Matrix3 matrix)
    {
        if (!ShowDocks)
            return;

        const float DockScale = 1f;

        if (_docks.TryGetValue(uid, out var docks))
        {
            foreach (var state in docks)
            {
                var position = state.Coordinates.Position;
                var uiPosition = matrix.Transform(position);

                if (uiPosition.Length() > WorldRange - DockScale)
                    continue;

                var color = HighlightedDock == state.Entity ? state.HighlightedColor : state.Color;

                var verts = new[]
                {
                    matrix.Transform(position + new Vector2(-DockScale, -DockScale)),
                    matrix.Transform(position + new Vector2(DockScale, -DockScale)),
                    matrix.Transform(position + new Vector2(DockScale, DockScale)),
                    matrix.Transform(position + new Vector2(-DockScale, DockScale)),
                };

                for (var i = 0; i < verts.Length; i++)
                {
                    var vert = verts[i];
                    vert.Y = -vert.Y;
                    verts[i] = ScalePosition(vert);
                }

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, color.WithAlpha(0.8f));
                handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
            }
        }
    }

    private void DrawGrid(DrawingHandleScreen handle, Matrix3 matrix, Entity<MapGridComponent> grid, Color color, bool drawInterior)
    {
        var rator = _maps.GetAllTilesEnumerator(grid.Owner, grid.Comp);
        var edges = new ValueList<Vector2>();
        var tileTris = new ValueList<Vector2>();
        const bool DrawInterior = true;

        while (rator.MoveNext(out var tileRef))
        {
            // TODO: Short-circuit interior chunk nodes
            // This can be optimised a lot more if required.
            var tileVec = _maps.TileToVector(grid, tileRef.Value.GridIndices);

            /*
             * You may be wondering what the fuck is going on here.
             * Well you see originally I tried drawing the interiors by fixture, but the problem is
             * you get rounding issues and get noticeable aliasing (at least if you don't overdraw and use alpha).
             * Hence per-tile should alleviate it.
             */
            var bl = tileVec;
            var br = tileVec + new Vector2(grid.Comp.TileSize, 0f);
            var tr = tileVec + new Vector2(grid.Comp.TileSize, grid.Comp.TileSize);
            var tl = tileVec + new Vector2(0f, grid.Comp.TileSize);

            var adjustedBL = matrix.Transform(bl);
            var adjustedBR = matrix.Transform(br);
            var adjustedTR = matrix.Transform(tr);
            var adjustedTL = matrix.Transform(tl);

            var scaledBL = ScalePosition(new Vector2(adjustedBL.X, -adjustedBL.Y));
            var scaledBR = ScalePosition(new Vector2(adjustedBR.X, -adjustedBR.Y));
            var scaledTR = ScalePosition(new Vector2(adjustedTR.X, -adjustedTR.Y));
            var scaledTL = ScalePosition(new Vector2(adjustedTL.X, -adjustedTL.Y));

            if (DrawInterior)
            {
                // Draw 2 triangles for the quad.
                tileTris.Add(scaledBL);
                tileTris.Add(scaledBR);
                tileTris.Add(scaledTL);

                tileTris.Add(scaledBR);
                tileTris.Add(scaledTL);
                tileTris.Add(scaledTR);
            }

            // Iterate edges and see which we can draw
            for (var i = 0; i < 4; i++)
            {
                var dir = (DirectionFlag) Math.Pow(2, i);
                var dirVec = dir.AsDir().ToIntVec();

                if (!_maps.GetTileRef(grid.Owner, grid.Comp, tileRef.Value.GridIndices + dirVec).Tile.IsEmpty)
                    continue;

                Vector2 start;
                Vector2 end;
                Vector2 actualStart;
                Vector2 actualEnd;

                // Draw line
                // Could probably rotate this but this might be faster?
                switch (dir)
                {
                    case DirectionFlag.South:
                        start = adjustedBL;
                        end = adjustedBR;

                        actualStart = scaledBL;
                        actualEnd = scaledBR;
                        break;
                    case DirectionFlag.East:
                        start = adjustedBR;
                        end = adjustedTR;

                        actualStart = scaledBR;
                        actualEnd = scaledTR;
                        break;
                    case DirectionFlag.North:
                        start = adjustedTR;
                        end = adjustedTL;

                        actualStart = scaledTR;
                        actualEnd = scaledTL;
                        break;
                    case DirectionFlag.West:
                        start = adjustedTL;
                        end = adjustedBL;

                        actualStart = scaledTL;
                        actualEnd = scaledBL;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (start.Length() > ActualRadarRange || end.Length() > ActualRadarRange)
                    continue;

                edges.Add(actualStart);
                edges.Add(actualEnd);
            }
        }

        if (DrawInterior)
        {
            handle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, tileTris.Span, color.WithAlpha(0.05f));
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.LineList, edges.Span, color);
    }

    private Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidPointVector;
    }

    private Vector2 InverseScalePosition(Vector2 value)
    {
        return (value - MidPointVector) / MinimapScale;
    }
}
