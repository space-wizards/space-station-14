using Content.Client.Stylesheets;
using Content.Shared.Shuttles.BUIStates;
using Content.Shared.Shuttles.Components;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Displays nearby grids inside of a control.
/// </summary>
public sealed class RadarControl : Control
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private const float ScrollSensitivity = 8f;
    private const float GridLinesDistance = 32f;

    /// <summary>
    /// Used to transform all of the radar objects. Typically is a shuttle console parented to a grid.
    /// </summary>
    private EntityCoordinates? _coordinates;

    private Angle? _rotation;

    private float _radarMinRange = 64f;
    private float _radarMaxRange = 256f;
    public float RadarRange { get; private set; } = 256f;

    /// <summary>
    /// We'll lerp between the radarrange and actual range
    /// </summary>
    private float _actualRadarRange = 256f;

    /// <summary>
    /// Controls the maximum distance that IFF labels will display.
    /// </summary>
    public float MaxRadarRange { get; private set; } = 256f * 10f;

    private int MinimapRadius => (int) Math.Min(Size.X, Size.Y) / 2;
    private Vector2 MidPoint => (Size / 2) * UIScale;
    private int SizeFull => (int) (MinimapRadius * 2 * UIScale);
    private int ScaledMinimapRadius => (int) (MinimapRadius * UIScale);
    private float MinimapScale => RadarRange != 0 ? ScaledMinimapRadius / RadarRange : 0f;

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
    public EntityUid? HighlightedDock;

    public Action<float>? OnRadarRangeChanged;

    public RadarControl()
    {
        IoCManager.InjectDependencies(this);
        MinSize = (SizeFull, SizeFull);
        RectClipContent = true;
    }

    public void SetMatrix(EntityCoordinates? coordinates, Angle? angle)
    {
        _coordinates = coordinates;
        _rotation = angle;
    }

    public void UpdateState(RadarConsoleBoundInterfaceState ls)
    {
        _radarMaxRange = ls.MaxRange;

        if (_radarMaxRange < RadarRange)
        {
            _actualRadarRange = _radarMaxRange;
        }

        if (_radarMaxRange < _radarMinRange)
            _radarMinRange = _radarMaxRange;

        _docks.Clear();

        foreach (var state in ls.Docks)
        {
            var coordinates = state.Coordinates;
            var grid = _docks.GetOrNew(coordinates.EntityId);
            grid.Add(state);
        }
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        AddRadarRange(-args.Delta.Y * 1f / ScrollSensitivity * RadarRange);
    }

    public void AddRadarRange(float value)
    {
        _actualRadarRange = Math.Clamp(_actualRadarRange + value, _radarMinRange, _radarMaxRange);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!_actualRadarRange.Equals(RadarRange))
        {
            var diff = _actualRadarRange - RadarRange;
            var lerpRate = 10f;

            RadarRange += (float) Math.Clamp(diff, -lerpRate * MathF.Abs(diff) * _timing.FrameTime.TotalSeconds, lerpRate * MathF.Abs(diff) * _timing.FrameTime.TotalSeconds);
            OnRadarRangeChanged?.Invoke(RadarRange);
        }

        var fakeAA = new Color(0.08f, 0.08f, 0.08f);

        handle.DrawCircle((MidPoint.X, MidPoint.Y), ScaledMinimapRadius + 1, fakeAA);
        handle.DrawCircle((MidPoint.X, MidPoint.Y), ScaledMinimapRadius, Color.Black);

        // No data
        if (_coordinates == null || _rotation == null)
        {
            Clear();
            return;
        }

        var gridLines = new Color(0.08f, 0.08f, 0.08f);
        var gridLinesRadial = 8;
        var gridLinesEquatorial = (int) Math.Floor(RadarRange / GridLinesDistance);

        for (var i = 1; i < gridLinesEquatorial + 1; i++)
        {
            handle.DrawCircle((MidPoint.X, MidPoint.Y), GridLinesDistance * MinimapScale * i, gridLines, false);
        }

        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            var aExtent = angle.ToVec() * ScaledMinimapRadius;
            handle.DrawLine((MidPoint.X, MidPoint.Y) - aExtent, (MidPoint.X, MidPoint.Y) + aExtent, gridLines);
        }

        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var fixturesQuery = _entManager.GetEntityQuery<FixturesComponent>();
        var bodyQuery = _entManager.GetEntityQuery<PhysicsComponent>();

        var mapPosition = _coordinates.Value.ToMap(_entManager);

        if (mapPosition.MapId == MapId.Nullspace || !xformQuery.TryGetComponent(_coordinates.Value.EntityId, out var xform))
        {
            Clear();
            return;
        }

        var offset = _coordinates.Value.Position;
        var offsetMatrix = Matrix3.CreateInverseTransform(
            mapPosition.Position,
            xform.WorldRotation - _rotation.Value);

        // Draw our grid in detail
        var ourGridId = _coordinates.Value.GetGridUid(_entManager);
        if (ourGridId != null)
        {
            var ourGridMatrix = xformQuery.GetComponent(ourGridId.Value).WorldMatrix;
            var ourGridFixtures = fixturesQuery.GetComponent(ourGridId.Value);

            Matrix3.Multiply(in ourGridMatrix, in offsetMatrix, out var matrix);

            // Draw our grid; use non-filled boxes so it doesn't look awful.
            DrawGrid(handle, matrix, ourGridFixtures, Color.Yellow);

            DrawDocks(handle, ourGridId.Value, matrix);
        }

        var invertedPosition = _coordinates.Value.Position - offset;
        invertedPosition.Y = -invertedPosition.Y;
        // Don't need to transform the InvWorldMatrix again as it's already offset to its position.

        // Draw radar position on the station
        handle.DrawCircle(ScalePosition(invertedPosition), 5f, Color.Lime);

        var shown = new HashSet<EntityUid>();

        // Draw other grids... differently
        foreach (var grid in _mapManager.FindGridsIntersecting(mapPosition.MapId,
                     new Box2(mapPosition.Position - MaxRadarRange, mapPosition.Position + MaxRadarRange)))
        {
            if (grid.Owner == ourGridId || !fixturesQuery.TryGetComponent(grid.Owner, out var gridFixtures))
                continue;

            var gridBody = bodyQuery.GetComponent(grid.Owner);
            if (gridBody.Mass < 10f)
            {
                ClearLabel(grid.Owner);
                continue;
            }

            _entManager.TryGetComponent<IFFComponent>(grid.Owner, out var iff);

            // Hide it entirely.
            if (iff != null &&
                (iff.Flags & IFFFlags.Hide) != 0x0)
            {
                continue;
            }

            shown.Add(grid.Owner);
            var name = metaQuery.GetComponent(grid.Owner).EntityName;

            if (name == string.Empty)
                name = Loc.GetString("shuttle-console-unknown");

            var gridXform = xformQuery.GetComponent(grid.Owner);
            var gridMatrix = gridXform.WorldMatrix;
            Matrix3.Multiply(in gridMatrix, in offsetMatrix, out var matty);
            var color = iff?.Color ?? IFFComponent.IFFColor;

            if (ShowIFF &&
                (iff == null && IFFComponent.ShowIFFDefault ||
                 (iff.Flags & IFFFlags.HideLabel) == 0x0))
            {
                var gridBounds = grid.LocalAABB;
                Label label;

                if (!_iffControls.TryGetValue(grid.Owner, out var control))
                {
                    label = new Label()
                    {
                        HorizontalAlignment = HAlignment.Left,
                    };

                    _iffControls[grid.Owner] = label;
                    AddChild(label);
                }
                else
                {
                    label = (Label) control;
                }

                label.FontColorOverride = color;
                var gridCentre = matty.Transform(gridBody.LocalCenter);
                gridCentre.Y = -gridCentre.Y;
                var distance = gridCentre.Length;

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
                ClearLabel(grid.Owner);
            }

            // Detailed view
            DrawGrid(handle, matty, gridFixtures, color);

            DrawDocks(handle, grid.Owner, matty);
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
        if (!ShowDocks) return;

        const float DockScale = 1.2f;

        if (_docks.TryGetValue(uid, out var docks))
        {
            foreach (var state in docks)
            {
                var ent = state.Entity;
                var position = state.Coordinates.Position;
                var uiPosition = matrix.Transform(position);

                if (uiPosition.Length > RadarRange - DockScale) continue;

                var color = HighlightedDock == ent ? state.HighlightedColor : state.Color;

                uiPosition.Y = -uiPosition.Y;

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

                handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, color);
            }
        }
    }

    private void DrawGrid(DrawingHandleScreen handle, Matrix3 matrix, FixturesComponent component, Color color)
    {
        foreach (var fixture in component.Fixtures.Values)
        {
            // If the fixture has any points out of range we won't draw any of it.
            var invalid = false;
            var poly = (PolygonShape) fixture.Shape;
            var verts = new Vector2[poly.VertexCount + 1];

            for (var i = 0; i < poly.VertexCount; i++)
            {
                var vert = matrix.Transform(poly.Vertices[i]);

                if (vert.Length > RadarRange)
                {
                    invalid = true;
                    break;
                }

                vert.Y = -vert.Y;
                verts[i] = ScalePosition(vert);
            }

            if (invalid) continue;

            // Closed list
            verts[poly.VertexCount] = verts[0];
            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
        }
    }

    private Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidPoint;
    }
}
