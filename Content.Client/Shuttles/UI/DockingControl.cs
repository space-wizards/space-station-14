using Content.Shared.Shuttles.BUIStates;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;

namespace Content.Client.Shuttles.UI;

/// <summary>
/// Displays the docking view from a specific docking port
/// </summary>
[Virtual]
public class DockingControl : Control
{
    private readonly IEntityManager _entManager;
    private readonly IMapManager _mapManager;

    private float _range = 8f;
    private float _rangeSquared = 0f;
    private const float GridLinesDistance = 32f;

    private int MinimapRadius => (int) Math.Min(Size.X, Size.Y) / 2;

    private Vector2 MidPoint => (Size / 2) * UIScale;
    private int SizeFull => (int) (MinimapRadius * 2 * UIScale);
    private int ScaledMinimapRadius => (int) (MinimapRadius * UIScale);
    private float MinimapScale => _range != 0 ? ScaledMinimapRadius / _range : 0f;

    public EntityUid? ViewedDock;
    public EntityUid? GridEntity;

    public EntityCoordinates? Coordinates;
    public Angle? Angle;

    /// <summary>
    /// Stored by GridID then by docks
    /// </summary>
    public Dictionary<EntityUid, List<DockingInterfaceState>> Docks = new();

    public DockingControl()
    {
        _entManager = IoCManager.Resolve<IEntityManager>();
        _mapManager = IoCManager.Resolve<IMapManager>();
        _rangeSquared = _range * _range;
        MinSize = (SizeFull, SizeFull);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var fakeAA = new Color(0.08f, 0.08f, 0.08f);

        handle.DrawCircle((MidPoint.X, MidPoint.Y), ScaledMinimapRadius + 1, fakeAA);
        handle.DrawCircle((MidPoint.X, MidPoint.Y), ScaledMinimapRadius, Color.Black);

        var gridLines = new Color(0.08f, 0.08f, 0.08f);
        var gridLinesRadial = 8;
        var gridLinesEquatorial = (int) Math.Floor(_range / GridLinesDistance);

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

        if (Coordinates == null ||
            Angle == null ||
            !_entManager.TryGetComponent<TransformComponent>(GridEntity, out var gridXform)) return;

        var rotation = Matrix3.CreateRotation(-Angle.Value + Math.PI);
        var matrix = Matrix3.CreateTranslation(-Coordinates.Value.Position);

        // Draw the fixtures around the dock before drawing it
        if (_entManager.TryGetComponent<FixturesComponent>(GridEntity, out var fixtures))
        {
            foreach (var (_, fixture) in fixtures.Fixtures)
            {
                var poly = (PolygonShape) fixture.Shape;

                for (var i = 0; i < poly.VertexCount; i++)
                {
                    var start = matrix.Transform(poly.Vertices[i]);
                    var end = matrix.Transform(poly.Vertices[(i + 1) % poly.VertexCount]);

                    var startOut = start.LengthSquared > _rangeSquared;
                    var endOut = end.LengthSquared > _rangeSquared;

                    // We need to draw to the radar border so we'll cap the range,
                    // but if none of the verts are in range then just leave it.
                    if (startOut && endOut)
                        continue;

                    start.Y = -start.Y;
                    end.Y = -end.Y;

                    // If start is outside we draw capped from end to start
                    if (startOut)
                    {
                        // It's called Jobseeker now.
                        if (!MathHelper.TryGetIntersecting(start, end, _range, out var newStart)) continue;
                        start = newStart.Value;
                    }
                    // otherwise vice versa
                    else if (endOut)
                    {
                        if (!MathHelper.TryGetIntersecting(end, start, _range, out var newEnd)) continue;
                        end = newEnd.Value;
                    }

                    handle.DrawLine(ScalePosition(start), ScalePosition(end), Color.Goldenrod);
                }
            }
        }

        // Draw the dock's collision
        handle.DrawRect(new UIBox2(
            ScalePosition(rotation.Transform(new Vector2(-0.2f, -0.7f))),
            ScalePosition(rotation.Transform(new Vector2(0.2f, -0.5f)))), Color.Aquamarine);

        // Draw the dock itself
        handle.DrawRect(new UIBox2(
            ScalePosition(rotation.Transform(new Vector2(-0.5f, 0.5f))),
            ScalePosition(rotation.Transform(new Vector2(0.5f, -0.5f)))), Color.Green);

        // Draw nearby grids
        var worldPos = gridXform.WorldMatrix.Transform(Coordinates.Value.Position);
        var gridInvMatrix = gridXform.InvWorldMatrix;
        Matrix3.Multiply(in gridInvMatrix, in matrix, out var invMatrix);

        // TODO: Getting some overdraw so need to fix that.
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();

        foreach (var grid in _mapManager.FindGridsIntersecting(gridXform.MapID,
                     new Box2(worldPos - _range, worldPos + _range)))
        {
            if (grid.Owner == GridEntity)
                continue;

            // Draw the fixtures before drawing any docks in range.
            if (!_entManager.TryGetComponent<FixturesComponent>(grid.Owner, out var gridFixtures))
                continue;

            var gridMatrix = xformQuery.GetComponent(grid.Owner).WorldMatrix;

            Matrix3.Multiply(in gridMatrix, in invMatrix, out var matty);

            foreach (var (_, fixture) in gridFixtures.Fixtures)
            {
                var poly = (PolygonShape) fixture.Shape;

                for (var i = 0; i < poly.VertexCount; i++)
                {
                    // This is because the same line might be on different fixtures so we don't want to draw it twice.
                    var startPos = poly.Vertices[i];
                    var endPos = poly.Vertices[(i + 1) % poly.VertexCount];

                    var start = matty.Transform(startPos);
                    var end = matty.Transform(endPos);

                    var startOut = start.LengthSquared > _rangeSquared;
                    var endOut = end.LengthSquared > _rangeSquared;

                    // We need to draw to the radar border so we'll cap the range,
                    // but if none of the verts are in range then just leave it.
                    if (startOut && endOut)
                        continue;

                    start.Y = -start.Y;
                    end.Y = -end.Y;

                    // If start is outside we draw capped from end to start
                    if (startOut)
                    {
                        // It's called Jobseeker now.
                        if (!MathHelper.TryGetIntersecting(start, end, _range, out var newStart)) continue;
                        start = newStart.Value;
                    }
                    // otherwise vice versa
                    else if (endOut)
                    {
                        if (!MathHelper.TryGetIntersecting(end, start, _range, out var newEnd)) continue;
                        end = newEnd.Value;
                    }

                    handle.DrawLine(ScalePosition(start), ScalePosition(end), Color.Aquamarine);
                }
            }

            // Draw any docks on that grid
            if (Docks.TryGetValue(grid.Owner, out var gridDocks))
            {
                foreach (var dock in gridDocks)
                {
                    var position = matty.Transform(dock.Coordinates.Position);

                    if (position.Length > _range - 0.8f) continue;

                    var otherDockRotation = Matrix3.CreateRotation(dock.Angle);

                    // Draw the dock's collision
                    var verts = new[]
                    {
                        matty.Transform(dock.Coordinates.Position +
                                        otherDockRotation.Transform(new Vector2(-0.2f, -0.7f))),
                        matty.Transform(dock.Coordinates.Position +
                                        otherDockRotation.Transform(new Vector2(0.2f, -0.7f))),
                        matty.Transform(dock.Coordinates.Position +
                                        otherDockRotation.Transform(new Vector2(0.2f, -0.5f))),
                        matty.Transform(dock.Coordinates.Position +
                                        otherDockRotation.Transform(new Vector2(-0.2f, -0.5f))),
                    };

                    for (var i = 0; i < verts.Length; i++)
                    {
                        var vert = verts[i];
                        vert.Y = -vert.Y;
                        verts[i] = ScalePosition(vert);
                    }

                    handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, Color.Turquoise);

                    // Draw the dock itself
                    verts = new[]
                    {
                        matty.Transform(dock.Coordinates.Position + new Vector2(-0.5f, -0.5f)),
                        matty.Transform(dock.Coordinates.Position + new Vector2(0.5f, -0.5f)),
                        matty.Transform(dock.Coordinates.Position + new Vector2(0.5f, 0.5f)),
                        matty.Transform(dock.Coordinates.Position + new Vector2(-0.5f, 0.5f)),
                    };

                    for (var i = 0; i < verts.Length; i++)
                    {
                        var vert = verts[i];
                        vert.Y = -vert.Y;
                        verts[i] = ScalePosition(vert);
                    }

                    handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, verts, Color.Green);
                }
            }
        }

    }

    private Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidPoint;
    }
}
