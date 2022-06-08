using Content.Client.Stylesheets;
using Content.Shared.Shuttles.BUIStates;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Client.Shuttles.UI;

public sealed class RadarControl : Control
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    private const int MinimapRadius = 384;
    private const int MinimapMargin = 4;
    private const float GridLinesDistance = 32f;

    /// <summary>
    /// Entity used to transform all of the radar objects.
    /// </summary>
    private EntityUid? _entity;

    private float _radarRange = 256f;

    private int SizeFull => (int) ((MinimapRadius + MinimapMargin) * 2 * UIScale);
    private int ScaledMinimapRadius => (int) (MinimapRadius * UIScale);
    private float MinimapScale => _radarRange != 0 ? ScaledMinimapRadius / _radarRange : 0f;

    /// <summary>
    /// Shows a label on each radar object.
    /// </summary>
    private Dictionary<EntityUid, Control> _iffControls = new();

    public bool ShowIFF { get; set; } = true;

    public RadarControl()
    {
        IoCManager.InjectDependencies(this);
        MinSize = (SizeFull, SizeFull);
    }

    public void UpdateState(RadarConsoleBoundInterfaceState ls)
    {
        _radarRange = ls.Range;
        _entity = ls.Entity;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        // TODO: Just draw shuttles in range on fixture normals.
        var point = SizeFull / 2;
        var fakeAA = new Color(0.08f, 0.08f, 0.08f);

        handle.DrawCircle((point, point), ScaledMinimapRadius + 1, fakeAA);
        handle.DrawCircle((point, point), ScaledMinimapRadius, Color.Black);

        // No data
        if (_entity == null)
        {
            foreach (var (_, label) in _iffControls)
            {
                label.Dispose();
            }

            _iffControls.Clear();
            return;
        }


        var gridLines = new Color(0.08f, 0.08f, 0.08f);
        var gridLinesRadial = 8;
        var gridLinesEquatorial = (int) Math.Floor(_radarRange / GridLinesDistance);

        for (var i = 1; i < gridLinesEquatorial + 1; i++)
        {
            handle.DrawCircle((point, point), GridLinesDistance * MinimapScale * i, gridLines, false);
        }

        for (var i = 0; i < gridLinesRadial; i++)
        {
            Angle angle = (Math.PI / gridLinesRadial) * i;
            var aExtent = angle.ToVec() * ScaledMinimapRadius;
            handle.DrawLine((point, point) - aExtent, (point, point) + aExtent, gridLines);
        }

        var metaQuery = _entManager.GetEntityQuery<MetaDataComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var fixturesQuery = _entManager.GetEntityQuery<FixturesComponent>();
        var bodyQuery = _entManager.GetEntityQuery<PhysicsComponent>();
        var xform = xformQuery.GetComponent(_entity.Value);
        var mapPosition = xform.MapPosition;
        var matrix = xform.InvWorldMatrix;

        // Draw our grid in detail
        var ourGridId = xform.GridID;
        var ourGridFixtures = fixturesQuery.GetComponent(ourGridId);

        // Can also use ourGridBody.LocalCenter
        var offset = xform.Coordinates.Position;

        var invertedPosition = xform.Coordinates.Position - offset;
        invertedPosition.Y = -invertedPosition.Y;
        var offsetMatrix = Matrix3.CreateTranslation(-offset);

        // Draw our grid; use non-filled boxes so it doesn't look awful.
        DrawGrid(handle, offsetMatrix, ourGridFixtures, point, Color.Yellow);

        // Don't need to transform the InvWorldMatrix again as it's already offset to its position.

        // Draw radar position on the station
        handle.DrawCircle(invertedPosition * MinimapScale + point, 5f, Color.Lime);

        var shown = new HashSet<EntityUid>();

        // Draw other grids... differently
        foreach (var grid in _mapManager.FindGridsIntersecting(mapPosition.MapId,
                     new Box2(mapPosition.Position - _radarRange, mapPosition.Position + _radarRange)))
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
                name = "Unknown";

            var gridXform = xformQuery.GetComponent(grid.GridEntityId);
            var gridFixtures = fixturesQuery.GetComponent(grid.GridEntityId);
            var gridMatrix = gridXform.WorldMatrix;
            Matrix3.Multiply(ref gridMatrix, ref matrix, out var matty);

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

                if (gridCentre.Length < _radarRange)
                {
                    control.Visible = true;
                    var label = (Label) control.GetChild(0);
                    label.Text = $"{name} ({gridCentre.Length:0.0}m)";
                    LayoutContainer.SetPosition(control, (gridCentre * MinimapScale + point ) / UIScale);
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
            DrawGrid(handle, matty, gridFixtures, point, Color.Aquamarine);
        }

        foreach (var (ent, _) in _iffControls)
        {
            if (shown.Contains(ent)) continue;
            ClearLabel(ent);
        }
    }

    private void ClearLabel(EntityUid uid)
    {
        if (!_iffControls.TryGetValue(uid, out var label)) return;
        label.Dispose();
        _iffControls.Remove(uid);
    }

    private void DrawGrid(DrawingHandleScreen handle, Matrix3 matrix, FixturesComponent component, int point, Color color)
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

                if (vert.Length > _radarRange)
                {
                    invalid = true;
                    break;
                }

                vert.Y = -vert.Y;
                verts[i] = vert * MinimapScale + point;
            }

            if (invalid) continue;

            // Closed list
            verts[poly.VertexCount] = verts[0];
            handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, color);
        }
    }
}