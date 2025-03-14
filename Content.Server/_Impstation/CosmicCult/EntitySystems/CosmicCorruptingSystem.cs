using Content.Shared.Maps;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Numerics;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Shared.Tag;
using Content.Server._Impstation.CosmicCult.Components;

namespace Content.Server._Impstation.CosmicCult.EntitySystems;
public sealed class CosmicCorruptingSystem : EntitySystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TurfSystem _turfs = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IRobustRandom _rand = default!;
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var blanktimer = EntityQueryEnumerator<CosmicCorruptingComponent>();
        while (blanktimer.MoveNext(out var uid, out var comp))
        {
            if (comp.Enabled && _timing.CurTime >= comp.CorruptionTimer)
            {
                comp.CorruptionTimer = _timing.CurTime + comp.CorruptionSpeed;
                ConvertTilesInRange((uid, comp));
                if (comp.CorruptionGrowth && comp.CorruptionRadius <= comp.CorruptionMaxRadius)
                {
                    comp.CorruptionRadius++;
                    comp.CorruptionChance -= comp.CorruptionReduction;
                }
                if (comp.CorruptionRadius >= comp.CorruptionMaxRadius)
                    comp.CorruptionGrowth = false;
                if (comp.CorruptionRadius >= comp.CorruptionMaxRadius && comp.AutoDisable)
                    comp.Enabled = false;
            }
        }
    }

    private void ConvertTilesInRange(Entity<CosmicCorruptingComponent> uid)
    {
        var tgtPos = Transform(uid);
        if (tgtPos.GridUid is not { } gridUid || !TryComp(gridUid, out MapGridComponent? mapGrid))
            return;

        var radius = uid.Comp.CorruptionRadius;
        var tileEnumerator = _map.GetLocalTilesEnumerator(gridUid, mapGrid, new Box2(tgtPos.Coordinates.Position + new Vector2(-radius, -radius), tgtPos.Coordinates.Position + new Vector2(radius, radius)));
        var entityHash = _lookup.GetEntitiesInRange(Transform(uid).Coordinates, radius);
        var convertTile = (ContentTileDefinition)_tileDefinition[uid.Comp.ConversionTile];
        foreach (var entity in entityHash)
        {
            if (TryComp<TagComponent>(entity, out var tag))
            {
                var tags = tag.Tags;
                if (tags.Contains("Wall") && Prototype(entity) != null && Prototype(entity)!.ID != uid.Comp.ConversionWall && _rand.Prob(uid.Comp.CorruptionChance))
                {
                    Spawn(uid.Comp.ConversionWall, Transform(entity).Coordinates);
                    if (uid.Comp.UseVFX)
                        Spawn(uid.Comp.TileConvertVFX, Transform(entity).Coordinates);
                    QueueDel(entity);
                }
            }
        }
        while (tileEnumerator.MoveNext(out var tile))
        {
            var tilePos = _turfs.GetTileCenter(tile);
            if (tile.Tile.TypeId == convertTile.TileId)
                continue;
            if (tile.GetContentTileDefinition().Name != convertTile.Name && _rand.Prob(uid.Comp.CorruptionChance))
            {
                _tile.ReplaceTile(tile, convertTile);
                _tile.PickVariant(convertTile);
                if (uid.Comp.UseVFX)
                    Spawn(uid.Comp.TileConvertVFX, tilePos);
            }
        }
    }
}
