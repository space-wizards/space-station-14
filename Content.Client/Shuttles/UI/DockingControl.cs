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
    private IEntityManager _entManager;
    private IMapManager _mapManager;

    private const int MinimapRadius = 384;
    private const int MinimapMargin = 4;

    private float _range = 8f;
    private float _rangeSquared = 0f;

    private int MidPoint => SizeFull / 2;
    private int SizeFull => (int) ((MinimapRadius + MinimapMargin) * 2 * UIScale);
    private int ScaledMinimapRadius => (int) (MinimapRadius * UIScale);
    private float MinimapScale => _range != 0 ? ScaledMinimapRadius / _range : 0f;

    public EntityUid? Entity;
    public EntityUid? GridEntity;

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

        handle.DrawCircle((MidPoint, MidPoint), ScaledMinimapRadius + 1, fakeAA);
        handle.DrawCircle((MidPoint, MidPoint), ScaledMinimapRadius, Color.Black);

        if (!_entManager.TryGetComponent<TransformComponent>(Entity, out var xform) ||
            !_entManager.TryGetComponent<TransformComponent>(GridEntity, out var gridXform)) return;

        var rotation = Matrix3.CreateRotation(xform.LocalRotation);
        var matrix = Matrix3.CreateTranslation(-xform.LocalPosition);

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
            ScalePosition(rotation.Transform(new Vector2(0.2f, -0.5f)))), Color.Aquamarine, false);

        // Draw the dock itself
        handle.DrawLine(
            ScalePosition(rotation.Transform(new Vector2(-0.5f, -0.5f))),
            ScalePosition(rotation.Transform(new Vector2(0.5f, -0.5f))), Color.Green);

        // Draw nearby grids
        var worldPos = gridXform.WorldMatrix.Transform(xform.LocalPosition);
        var gridInvMatrix = gridXform.InvWorldMatrix;
        Matrix3.Multiply(in gridInvMatrix, in matrix, out var invMatrix);

        // TODO: Getting some overdraw so need to fix that.

        foreach (var grid in _mapManager.FindGridsIntersecting(xform.MapID,
                     new Box2(worldPos - _range, worldPos + _range)))
        {
            if (grid.GridEntityId == GridEntity) continue;

            // Draw the fixtures before drawing any docks in range.
            if (!_entManager.TryGetComponent<FixturesComponent>(grid.GridEntityId, out var gridFixtures)) continue;

            var gridMatrix = grid.WorldMatrix;

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
            // foreach (var dock in )
        }

    }

    private Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidPoint;
    }
}
