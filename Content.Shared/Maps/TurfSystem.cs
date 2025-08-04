using System.Numerics;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

namespace Content.Shared.Maps;

/// <summary>
///     This system provides various useful helper methods for turfs & tiles. Replacement for <see cref="TurfHelpers"/>
/// </summary>
public sealed class TurfSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    ///     Returns true if a given tile is blocked by physics-enabled entities.
    /// </summary>
    public bool IsTileBlocked(TileRef turf, CollisionGroup mask, float minIntersectionArea = 0.1f)
        => IsTileBlocked(turf.GridUid, turf.GridIndices, mask, minIntersectionArea: minIntersectionArea);

    /// <summary>
    ///     Returns true if a given tile is blocked by physics-enabled entities.
    /// </summary>
    /// <param name="gridUid">The grid that owns the tile</param>
    /// <param name="indices">The tile indices</param>
    /// <param name="mask">Collision layers to check</param>
    /// <param name="grid">Grid component</param>
    /// <param name="gridXform">Grid's transform</param>
    /// <param name="minIntersectionArea">Minimum area that must be covered for a tile to be considered blocked</param>
    public bool IsTileBlocked(EntityUid gridUid,
        Vector2i indices,
        CollisionGroup mask,
        MapGridComponent? grid = null,
        TransformComponent? gridXform = null,
        float minIntersectionArea = 0.1f)
    {
        if (!Resolve(gridUid, ref grid, ref gridXform))
            return false;

        var xformQuery = GetEntityQuery<TransformComponent>();
        var (gridPos, gridRot, matrix) = _transform.GetWorldPositionRotationMatrix(gridXform, xformQuery);

        var size = grid.TileSize;
        var localPos = new Vector2(indices.X * size + (size / 2f), indices.Y * size + (size / 2f));
        var worldPos = Vector2.Transform(localPos, matrix);

        // This is scaled to 95 % so it doesn't encompass walls on other tiles.
        var tileAabb = Box2.UnitCentered.Scale(0.95f * size);
        var worldBox = new Box2Rotated(tileAabb.Translated(worldPos), gridRot, worldPos);
        tileAabb = tileAabb.Translated(localPos);

        var intersectionArea = 0f;
        var fixtureQuery = GetEntityQuery<FixturesComponent>();
        foreach (var ent in _entityLookup.GetEntitiesIntersecting(gridUid, worldBox, LookupFlags.Dynamic | LookupFlags.Static))
        {
            if (!fixtureQuery.TryGetComponent(ent, out var fixtures))
                continue;

            // get grid local coordinates
            var (pos, rot) = _transform.GetWorldPositionRotation(xformQuery.GetComponent(ent), xformQuery);
            rot -= gridRot;
            pos = (-gridRot).RotateVec(pos - gridPos);

            var xform = new Transform(pos, (float) rot.Theta);

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (!fixture.Hard)
                    continue;

                if ((fixture.CollisionLayer & (int) mask) == 0)
                    continue;

                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    var intersection = fixture.Shape.ComputeAABB(xform, i).Intersect(tileAabb);
                    intersectionArea += intersection.Width * intersection.Height;
                    if (intersectionArea > minIntersectionArea)
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the location of the centre of the tile in grid coordinates.
    /// </summary>
    public EntityCoordinates GetTileCenter(TileRef turf)
    {
        var grid = Comp<MapGridComponent>(turf.GridUid);
        var center = (turf.GridIndices + new Vector2(0.5f, 0.5f)) * grid.TileSize;
        return new EntityCoordinates(turf.GridUid, center);
    }
}
