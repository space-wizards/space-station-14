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
using Content.Server.Shuttles.Components;
using Content.Shared.Doors.Components;
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
        new Vector2i(0,0),
        new Vector2i(1, 0),
        new Vector2i(-1, -1),
        new Vector2i(0, -1),
        new Vector2i(1, -1),
    ];

    /// <remarks>
    /// this system is a mostly generic way of replacing tiles around an entity. the only hardcoded behaviour is secret walls -> malign doors, but that shouldn't be too hard to fix if this is needed for smth else later.
    /// </remarks>
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
                ConvertTiles((uid, comp));
                if (comp.CorruptionTicks <= comp.CorruptionMaxTicks)
                {
                    comp.CorruptionTicks++;
                    comp.CorruptionChance -= comp.CorruptionReduction;
                }
                if (comp.CorruptionTicks >= comp.CorruptionMaxTicks && comp.AutoDisable)
                    comp.Enabled = false; //maybe just remComp this? atm nothing re-enables a corruptor so that should be safe to do?
            }
        }
    }
    private void ConvertTiles(Entity<CosmicCorruptingComponent> uid)
    {
        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var mapGrid))
            return;
        var convertTile = (ContentTileDefinition)_tileDefinition[uid.Comp.ConversionTile];

        if (uid.Comp.Mobile) //if this is a mobile corruptor, reset the list of corruptable tiles every attempt. not a super clean solution because I didn't account for the astral nova in the first rewrite but it works fine.
        {
            uid.Comp.CorruptableTiles.Clear();
            var tile = _map.GetTileRef((gridUid, mapGrid), xform.Coordinates);
            //add every neighbouring tile to the corruptable list
            foreach (var neighbourPos in _neighbourPositions)
            {
                var neighbourRef = _map.GetTileRef((gridUid, mapGrid), tile.GridIndices + neighbourPos);
                if (neighbourRef.Tile.TypeId == convertTile.TileId)
                    continue; //ignore already converted tiles
                uid.Comp.CorruptableTiles.Add(neighbourRef.GridIndices);
            }
        }

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
                    var proto = Prototype(convertedEnt);
                    if (tags.Contains("Wall") && proto != null && (proto.ID != uid.Comp.ConversionWall || proto.ID != uid.Comp.ConversionDoor)) //if we hit something that isn't already converted & can be, convert it
                    {
                        //the secret door check (if has "wall" tag && a doorComponent) is a little hacky & heuristic.
                        //ideally there'd be a <protoID, protoID> hashmap for this & we wouldn't be checking tags or anything, but that's a lot of manual data entry that I can foist onto future me, she's a sucker for that kinda thing - ruddygreat
                        //also not using a ternary here because this is nicer to read in my imo
                        if (TryComp<DoorComponent>(convertedEnt, out _))
                        {
                            Spawn(uid.Comp.ConversionDoor, Transform(convertedEnt).Coordinates);
                        }
                        else
                        {
                            Spawn(uid.Comp.ConversionWall, Transform(convertedEnt).Coordinates);
                        }
                        QueueDel(convertedEnt);
                    }
                }
                //spawn the vfx if we should
                if (uid.Comp.UseVFX)
                    Spawn(uid.Comp.TileConvertVFX, _turfs.GetTileCenter(tileRef));
                uid.Comp.CorruptableTiles.Remove(pos);
            }
        }
    }
}
