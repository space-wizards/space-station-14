using Robust.Shared.Map.Components;

namespace Content.Server.Anomaly;

/// <summary>
/// This handles anomalous vessel as well as
/// the calculations for how many points they
/// should produce.
/// </summary>
public sealed partial class AnomalySystem
{
    private void InitializeGenerator()
    {
    }

    public void SpawnRandomAnomaly(EntityUid grid, string toSpawn)
    {
        if (!TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var xform = Transform(grid);

        var targetCoords = xform.Coordinates;
        var (gridPos, _, gridMatrix) = xform.GetWorldPositionRotationMatrix();
        var gridBounds = gridMatrix.TransformBox(gridComp.LocalAABB);

        for (var i = 0; i < 10; i++)
        {
            var randomX = _random.Next((int) gridBounds.Left, (int) gridBounds.Right);
            var randomY = _random.Next((int) gridBounds.Bottom, (int) gridBounds.Top);

            var tile = new Vector2i(randomX - (int) gridPos.X, randomY - (int) gridPos.Y);
            if (_atmosphere.IsTileSpace(gridComp.Owner, Transform(grid).MapUid, tile,
                    mapGridComp: gridComp) || _atmosphere.IsTileAirBlocked(gridComp.Owner, tile, mapGridComp: gridComp))
            {
                continue;
            }

            targetCoords = gridComp.GridTileToLocal(tile);
            break;
        }

        Spawn(toSpawn, targetCoords);
    }
}
