using System.Linq;
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

    private readonly HashSet<Vector2i> _neighbourPositions =
    [
        new Vector2i(-1, 1),
        new Vector2i(0, 1),
        new Vector2i(1, 1),
        new Vector2i(-1, 0),
        new Vector2i(1, 0),
        new Vector2i(-1, -1),
        new Vector2i(0, -1),
        new Vector2i(1, -1),
    ];


    public override void Initialize()
    {
        SubscribeLocalEvent<CosmicCorruptingComponent, MapInitEvent>(OnMapInit);
    }

    //when the entity spawns, convert the tile under it & add all neighbouring tiles to the corruptable list
    private void OnMapInit(Entity<CosmicCorruptingComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;

        var grid = (gridUid, mapGrid);
        var tile = _map.GetTileRef(grid, xform.Coordinates);
        var convertTile = (ContentTileDefinition)_tileDefinition[ent.Comp.ConversionTile];
        _tile.ReplaceTile(tile, convertTile);

        //add every neighbouring tile to the corruptable list
        foreach (var neighbourPos in _neighbourPositions)
        {
            var neighbourRef = _map.GetTileRef((gridUid, mapGrid), tile.GridIndices + neighbourPos);
            if (neighbourRef.Tile.TypeId == convertTile.TileId)
                continue; //ignore already converted tiles

            ent.Comp.CorruptableTiles.Add(neighbourRef.GridIndices);
        }
    }

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
        var convertTile = (ContentTileDefinition)_tileDefinition[uid.Comp.ConversionTile];

        //go over every corruptible tile
        foreach (var pos in new HashSet<Vector2i>(uid.Comp.CorruptableTiles)) //we love avoiding ConcurrentModificationExceptions
        {
            var tileRef = _map.GetTileRef((gridUid, mapGrid), pos);
            if (tileRef.Tile.TypeId == convertTile.TileId) //if it's already corrupted, remove it from the list and continue
            {
                uid.Comp.CorruptableTiles.Remove(pos);
                continue;
            }

            if (_rand.Prob(uid.Comp.CorruptionChance)) //if it rolls good
            {
                //replace & variantise the tile
                _tile.ReplaceTile(tileRef, convertTile);
                _tile.PickVariant(convertTile);

                //then add the new neighbours as targets as long as they're not already corrupted
                foreach (var neighbourPos in _neighbourPositions)
                {
                    var neighbourRef = _map.GetTileRef((gridUid, mapGrid), tileRef.GridIndices + neighbourPos);
                    if (neighbourRef.Tile.TypeId == convertTile.TileId)
                        continue;

                    uid.Comp.CorruptableTiles.Add(neighbourRef.GridIndices);
                }

                //corrupt anything that can be corrupted
                foreach (var convertedEnt in _map.GetAnchoredEntities((gridUid, mapGrid), pos).ToList())
                {
                    if (!TryComp<TagComponent>(convertedEnt, out var tagComp))
                        continue;

                    var tags = tagComp.Tags; //I hate tags
                    if (!tags.Contains("Wall") || Prototype(convertedEnt) == null || Prototype(convertedEnt)!.ID == uid.Comp.ConversionWall)
                        continue;

                    Spawn(uid.Comp.ConversionWall, Transform(convertedEnt).Coordinates);
                    QueueDel(convertedEnt);
                }

                //spawn the vfx if we should
                if (uid.Comp.UseVFX)
                    Spawn(uid.Comp.TileConvertVFX, _turfs.GetTileCenter(tileRef));

                uid.Comp.CorruptableTiles.Remove(pos);
            }
        }
    }
}
