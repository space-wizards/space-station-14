using Content.Client.Stylesheets;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Utility;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Displays nearby grids inside of a control.
/// </summary>
public sealed class RadarControl : Control
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private const float ScrollSensitivity = 8f;

    private const int MinimapRadius = 384;
    private const int MinimapMargin = 4;
    private const float GridLinesDistance = 32f;

    /// <summary>
    /// Entity used to transform all of the radar objects.
    /// </summary>
    private EntityUid? _entity;

    private float _radarMinRange = 64f;
    private float _radarMaxRange = 256f;
    public float RadarRange { get; private set; } = 256f;

    private int MidPoint => SizeFull / 2;
    private int SizeFull => (int) ((MinimapRadius + MinimapMargin) * 2 * UIScale);
    private int ScaledMinimapRadius => (int) (MinimapRadius * UIScale);
    private float MinimapScale => RadarRange != 0 ? ScaledMinimapRadius / RadarRange : 0f;

    /// <summary>
    /// Shows a label on each radar object.
    /// </summary>
    private Dictionary<EntityUid, Control> _iffControls = new();

    private Dictionary<EntityUid, Dictionary<EntityUid, DockingInterfaceState>> _docks = new();

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
    }

    public void UpdateState(RadarConsoleBoundInterfaceState ls)
    {
        _radarMaxRange = ls.MaxRange;

        if (_radarMaxRange < RadarRange)
        {
            RadarRange = _radarMaxRange;
            OnRadarRangeChanged?.Invoke(RadarRange);
        }

        if (_radarMaxRange < _radarMinRange)
            _radarMinRange = _radarMaxRange;

        _entity = ls.Entity;
        _docks.Clear();

        foreach (var state in ls.Docks)
        {
            var coordinates = state.Coordinates;
            var grid = _docks.GetOrNew(coordinates.EntityId);
            grid.Add(state.Entity, state);
        }
    }

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        AddRadarRange(-args.Delta.Y * ScrollSensitivity);
    }

    public void AddRadarRange(float value)
    {
        var oldValue = RadarRange;
        RadarRange = MathF.Max(0f, MathF.Max(_radarMinRange, MathF.Min(RadarRange + value, _radarMaxRange)));

        if (oldValue.Equals(RadarRange)) return;

        OnRadarRangeChanged?.Invoke(RadarRange);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var fakeAA = new Color(0.08f, 0.08f, 0.08f);

        handle.DrawCircle((MidPoint, MidPoint), ScaledMinimapRadius + 1, fakeAA);
        handle.DrawCircle((MidPoint, MidPoint), ScaledMinimapRadius, Color.Black);

        // No data
        if (_entity == null)
        {
            Clear();
            return;
        }

        var gridLines = new Color(0.08f, 0.08f, 0.08f);
        var gridLinesRadial = 8;
        var gridLinesEquatorial = (int) Math.Floor(RadarRange / GridLinesDistance);

        for (var i = 1; i < gridLinesEquatorial + 1; i++)
        {
            handle.DrawCircle((MidPoint, MidPoint), GridLinesDistance * MinimapScale * i, gridLines, false);
        }

        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            var aExtent = angle.ToVec() * ScaledMinimapRadius;
            handle.DrawLine((MidPoint, MidPoint) - aExtent, (MidPoint, MidPoint) + aExtent, gridLines);
        }

        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var fixturesQuery = _entManager.GetEntityQuery<FixturesComponent>();
        var bodyQuery = _entManager.GetEntityQuery<PhysicsComponent>();
        var xform = xformQuery.GetComponent(_entity.Value);
        var mapPosition = xform.MapPosition;

        if (mapPosition.MapId == MapId.Nullspace)
        {
            Clear();
            return;
        }

        // Can also use ourGridBody.LocalCenter
        var offset = xform.Coordinates.Position;
        var offsetMatrix = Matrix3.CreateTranslation(-offset);
        Matrix3 matrix;

        // Draw our grid in detail
        var ourGridId = xform.GridID;
        if (ourGridId != GridId.Invalid)
        {
            matrix = xform.InvWorldMatrix;
            var ourGridFixtures = fixturesQuery.GetComponent(ourGridId);
            // Draw our grid; use non-filled boxes so it doesn't look awful.
            DrawGrid(handle, offsetMatrix, ourGridFixtures, Color.Yellow);

            DrawDocks(handle, xform.GridEntityId, offsetMatrix);
        }
        else
        {
            matrix = Matrix3.CreateTranslation(-offset);
        }

        var invertedPosition = xform.Coordinates.Position - offset;
        invertedPosition.Y = -invertedPosition.Y;
        // Don't need to transform the InvWorldMatrix again as it's already offset to its position.

        // Draw radar position on the station
        handle.DrawCircle(ScalePosition(invertedPosition), 5f, Color.Lime);

        var shown = new HashSet<EntityUid>();

        // Draw other grids... differently
        foreach (var grid in _mapManager.FindGridsIntersecting(mapPosition.MapId,
                     new Box2(mapPosition.Position - RadarRange, mapPosition.Position + RadarRange)))
        {
            if (grid.Index == ourGridId) continue;

            var gridBody = bodyQuery.GetComponent(grid.GridEntityId);
            if (gridBody.Mass < 10f)
            {
                ClearLabel(grid.GridEntityId);
                continue;
            }

            shown.Add(grid.GridEntityId);
            var name = metaQuery.GetComponent(grid.GridEntityId).EntityName;

            if (name == string.Empty)
                name = Loc.GetString("shuttle-console-unknown");

            var gridXform = xformQuery.GetComponent(grid.GridEntityId);
            var gridFixtures = fixturesQuery.GetComponent(grid.GridEntityId);
            var gridMatrix = gridXform.WorldMatrix;
            Matrix3.Multiply(in gridMatrix, in matrix, out var matty);

            if (ShowIFF)
            {
                if (!_iffControls.TryGetValue(grid.GridEntityId, out var control))
                {
                    var label = new Label()
                    {
                        HorizontalAlignment = HAlignment.Left,
                    };

                    control = new PanelContainer()
                    {
                        HorizontalAlignment = HAlignment.Left,
                        VerticalAlignment = VAlignment.Top,
                        Children = { label },
                        StyleClasses  = { StyleNano.StyleClassTooltipPanel },
                    };

                    _iffControls[grid.GridEntityId] = control;
                    AddChild(control);
                }

                var gridCentre = matty.Transform(gridBody.LocalCenter);
                gridCentre.Y = -gridCentre.Y;

                // TODO: When we get IFF or whatever we can show controls at a further distance; for now
                // we don't do that because it would immediately reveal nukies.
                if (gridCentre.Length < RadarRange)
                {
                    control.Visible = true;
                    var label = (Label) control.GetChild(0);
                    label.Text = Loc.GetString("shuttle-console-iff-label", ("name", name), ("distance", $"{gridCentre.Length:0.0}"));
                    LayoutContainer.SetPosition(control, ScalePosition(gridCentre) / UIScale);
                }
                else
                {
                    control.Visible = false;
                }
            }
            else
            {
                ClearLabel(grid.GridEntityId);
            }

            // Detailed view
            DrawGrid(handle, matty, gridFixtures, Color.Aquamarine);

            DrawDocks(handle, grid.GridEntityId, matty);
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
            foreach (var (ent, state) in docks)
            {
                var position = state.Coordinates.Position;
                var uiPosition = matrix.Transform(position);

                if (uiPosition.Length > RadarRange - DockScale) continue;

                var color = HighlightedDock == ent ? Color.Magenta : Color.DarkViolet;

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
        foreach (var (_, fixture) in component.Fixtures)
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
