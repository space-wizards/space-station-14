using Content.Shared.Radiation.Systems;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client.Radiation.Overlays;

public sealed class RadiationGridcastOverlay : Overlay
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;

    public List<RadiationRay>? Rays;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public RadiationGridcastOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        switch (args.Space)
        {
            case OverlaySpace.WorldSpace:
                DrawWorld(args);
                break;
        }
    }

    private void DrawWorld(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        if (Rays == null)
            return;

        // draw lines for raycasts
        foreach (var ray in Rays)
        {
            if (!ray.IsGridcast)
            {
                handle.DrawLine(ray.Source, ray.LastPos, Color.Red);
            }

        }

        // draw tiles for gridcasts
        foreach (var ray in Rays)
        {
            if (ray.Grid == null || !_mapManager.TryGetGrid(ray.Grid, out var grid))
                continue;
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
            var gridXform = xformQuery.GetComponent(grid.GridEntityId);
            var (_, _, worldMatrix, invWorldMatrix) = gridXform.GetWorldPositionRotationMatrixWithInv(xformQuery);
            var gridBounds = invWorldMatrix.TransformBox(args.WorldBounds);
            handle.SetTransform(worldMatrix);

            DrawTiles(handle, gridBounds, ray.VisitedTiles);

            handle.SetTransform(Matrix3.Identity);
        }
    }

    private void DrawTiles(DrawingHandleWorld handle, Box2 gridBounds,
        List<(Vector2i, float)> tiles, ushort tileSize = 1)
    {
        var color = Color.Green;
        color.A = 0.5f;

        foreach (var (tile, _) in tiles)
        {
            var centre = ((Vector2) tile + 0.5f) * tileSize;

            // is the center of this tile visible to the user?
            if (!gridBounds.Contains(centre))
                continue;

            var box = Box2.UnitCentered.Translated(centre);
            handle.DrawRect(box, color);
        }
    }
}
