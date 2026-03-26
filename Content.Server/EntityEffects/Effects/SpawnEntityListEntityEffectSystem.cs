using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Physics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Randomly picks entities from a list or lists to be spawned. Based on anomaly entity spawns.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T, TEffect}"/>
public sealed partial class SpawnEntityListEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnEntityListEffect>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly INetManager _net = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnEntityListEffect> args)
    {
        foreach (var entry in args.Effect.Entries)
        {
            var xform = Transform(entity);
            if (!TryComp(xform.GridUid, out MapGridComponent? grid))
                return;

            var tiles = GetSpawningPoints(entity, args.Scale * args.Effect.ResizeScale, entry.Settings);
            if (tiles == null)
                return;

            foreach (var tileref in tiles)
            {
                Spawn(_random.Pick(entry.Spawns), _mapSystem.ToCenterCoordinates(tileref, grid));
            }
        }
    }

    private List<TileRef>? GetSpawningPoints(EntityUid uid, float scale, EntityListSpawnSettings settings)
    {
        var xform = Transform(uid);

        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
            return null;

        // How many spawn points we will be aiming to return
        var amount = (int)(MathHelper.Lerp(settings.MinAmount, settings.MaxAmount, scale) + 0.5f);

        // When the entity is in a container or buckled, local coordinates will not be comparable to tile coordinates.
        // Get the world coordinates for the target entity
        var worldPos = _transformSystem.GetWorldPosition(uid);

        // Get a list of the tiles within the maximum range of the effect
        var tilerefs = _mapSystem.GetTilesIntersecting(
                xform.GridUid.Value,
                grid,
                new Box2(worldPos + new Vector2(-settings.MaxRange), worldPos + new Vector2(settings.MaxRange)))
            .ToList();

        if (tilerefs.Count == 0)
            return null;

        var physQuery = GetEntityQuery<PhysicsComponent>();
        var resultList = new List<TileRef>();
        while (resultList.Count < amount)
        {
            if (tilerefs.Count == 0)
                break;

            var tileref = _random.Pick(tilerefs);

            // Get the world position of the tile to calculate the distance to the target entity
            var tileWorldPos = _mapSystem.GridTileToWorldPos(xform.GridUid.Value, grid, tileref.GridIndices);
            var distance = Vector2.Distance(tileWorldPos, worldPos);

            //cut outer & inner circle
            if (distance > settings.MaxRange || distance < settings.MinRange)
            {
                tilerefs.Remove(tileref);
                continue;
            }

            if (!settings.CanSpawnOnEntities)
            {
                // If it can't spawn on entities, ensure that maximum one entity will be spawned here.
                tilerefs.Remove(tileref);

                var valid = true;
                foreach (var ent in _mapSystem.GetAnchoredEntities(xform.GridUid.Value, grid, tileref.GridIndices))
                {
                    if (!physQuery.TryGetComponent(ent, out var body))
                        continue;

                    if (body.BodyType != BodyType.Static ||
                        !body.Hard ||
                        (body.CollisionLayer & (int)CollisionGroup.Impassable) == 0)
                        continue;

                    valid = false;
                    break;
                }
                if (!valid)
                {
                    continue;
                }
            }

            resultList.Add(tileref);
        }
        return resultList;
    }
}
