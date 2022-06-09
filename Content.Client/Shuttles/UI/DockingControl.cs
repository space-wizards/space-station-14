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

    private float _range = 16f;

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

        var matrix = Matrix3.CreateTranslation(-xform.LocalPosition);
        var rotation = Matrix3.CreateRotation(xform.LocalRotation);

        // Draw the fixtures around the dock before drawing it
        if (_entManager.TryGetComponent<FixturesComponent>(GridEntity, out var fixtures))
        {
            var gridMatrix = gridXform.WorldMatrix;
            Matrix3.Multiply(in gridMatrix, in matrix, out var matty);

            foreach (var (_, fixture) in fixtures.Fixtures)
            {
                var invalid = false;
                var poly = (PolygonShape) fixture.Shape;
                var verts = new Vector2[poly.VertexCount + 1];

                for (var i = 0; i < poly.VertexCount; i++)
                {
                    var vert = matty.Transform(poly.Vertices[i]);

                    if (vert.Length > _range)
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
                handle.DrawPrimitives(DrawPrimitiveTopology.LineStrip, verts, Color.Yellow);
            }
        }

        // Draw the dock's collision
        handle.DrawRect(new UIBox2(
            ScalePosition(rotation.Transform(new Vector2(-0.2f, 0f) + xform.LocalPosition)),
            ScalePosition(rotation.Transform(new Vector2(0.2f, -0.2f) + xform.LocalPosition))), Color.Aquamarine, false);

        // Draw the dock itself
        handle.DrawLine(ScalePosition(rotation.Transform(new Vector2(-0.5f, 0f))), ScalePosition(rotation.Transform(new Vector2(0.5f, 0f))), Color.Orange);

        // Draw nearby grids
        var worldPos = xform.WorldPosition;

        foreach (var grid in _mapManager.FindGridsIntersecting(xform.MapID,
                     new Box2(worldPos - _range, worldPos + _range)))
        {

        }

    }

    private Vector2 ScalePosition(Vector2 value)
    {
        return value * MinimapScale + MidPoint;
    }
}
