using System.Linq;
using System.Runtime.InteropServices;
using Content.Server.Atmos.Components;
using Content.Server.Explosion.Components;
using Content.Shared.Atmos;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion;
using Content.Shared.FixedPoint;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Content.Server.Explosion.Components.ExplosionAirtightGridComponent;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem
{
    // We keep track of which tiles are airtight, and how much damage from explosions those airtight blockers can take.
    // This is quite complicated, as the data effectively needs to be tracked *per tile*, *per explosion type*.
    // To avoid wasting significant memory, we calculate the values and share the actual backing storage of it.
    // Stored values are reference counted so they can be evicted when no longer needed.
    // At the time of writing, this compacts the storage for Box Station from ~5500 tolerance value sets to 13,
    // at round start.

    // Use integers instead of prototype IDs for storage of explosion data.
    // This allows us to replace a Dictionary<string, FixedPoint2> with just a FixedPoint2[].
    private readonly Dictionary<ProtoId<ExplosionPrototype>, int> _explosionTypes = new();
    // Index to look up if we already have an existing set of tolerance values stored, so the data can be shared.
    private readonly Dictionary<ToleranceValues, int> _toleranceIndex = new();
    // Storage for tolerance values. Entries form a free linked list when not occupied by a set of real values.
    private ValueList<CacheEntry> _toleranceData;
    // First free position in _toleranceData.
    // -1 indicates there are no free slots left and the storage must be expanded.
    private int _freeListHead = -1;

    private void InitAirtightMap()
    {
        _explosionTypes.Clear();

        int index = 0;
        foreach (var prototype in _prototypeManager.EnumeratePrototypes<ExplosionPrototype>())
        {
            _explosionTypes.Add(prototype.ID, index);
            index++;
        }
    }

    private void ReloadExplosionPrototypes(PrototypesReloadedEventArgs prototypesReloadedEventArgs)
    {
        if (!prototypesReloadedEventArgs.Modified.Contains(typeof(ExplosionPrototype)))
            return;

        InitAirtightMap();
        ReloadMap();
    }

    public void UpdateAirtightMap(EntityUid gridId, Vector2i tile, MapGridComponent? grid = null)
    {
        if (Resolve(gridId, ref grid, false))
            UpdateAirtightMap(gridId, grid, tile);
    }

    [Access(typeof(ExplosionGridTileFlood))]
    public ToleranceValues GetToleranceValues(int idx)
    {
        return _toleranceData[idx].Values;
    }

    /// <summary>
    ///     Update the map of explosion blockers.
    /// </summary>
    /// <remarks>
    ///     Gets a list of all airtight entities on a tile. Assembles a <see cref="AtmosDirection"/> that specifies
    ///     what directions are blocked, along with the largest explosion tolerance. Note that as we only keep track
    ///     of the largest tolerance, this means that the explosion map will actually be inaccurate if you have
    ///     something like a normal and a reinforced windoor on the same tile. But given that this is a pretty rare
    ///     occurrence, I am fine with this.
    /// </remarks>
    public void UpdateAirtightMap(EntityUid gridId, MapGridComponent grid, Vector2i tile)
    {
        var airtightGrid = EnsureComp<ExplosionAirtightGridComponent>(gridId);

        // Calculate tile new airtight state.

        var tolerance = new FixedPoint2[_explosionTypes.Count];
        var blockedDirections = AtmosDirection.Invalid;

        var anchoredEnumerator = _map.GetAnchoredEntitiesEnumerator(gridId, grid, tile);

        while (anchoredEnumerator.MoveNext(out var uid))
        {
            if (!_airtightQuery.TryGetComponent(uid, out var airtight) || !airtight.AirBlocked)
                continue;

            blockedDirections |= airtight.AirBlockedDirection;
            GetExplosionTolerance(uid.Value, tolerance);
        }

        // Log.Info($"UPDATE {gridId}/{tile}: {blockedDirections}");

        if (blockedDirections == AtmosDirection.Invalid)
        {
            // No longer airtight

            if (!airtightGrid.Tiles.Remove(tile, out var tileData))
            {
                // Did not have this tile before and after, nothing to do.
                return;
            }

            // Removing tile data.
            DecrementRefCount(tileData.ToleranceCacheIndex);
            return;
        }

        ref var tileEntry = ref CollectionsMarshal.GetValueRefOrAddDefault(airtightGrid.Tiles, tile, out var existed);
        var cacheKey = new ToleranceValues { Values = tolerance };

        // Remove previous tolerance reference if necessary.
        if (existed)
        {
            ref var prevEntry = ref _toleranceData[tileEntry.ToleranceCacheIndex];
            if (prevEntry.Values == cacheKey)
            {
                // No change.
                return;
            }

            DecrementRefCount(tileEntry.ToleranceCacheIndex);
        }

        ref var newCacheIndex = ref CollectionsMarshal.GetValueRefOrAddDefault(_toleranceIndex, cacheKey, out existed);
        if (existed)
        {
            _toleranceData[newCacheIndex].RefCount += 1;
        }
        else
        {
            if (_freeListHead < 0)
                ExpandCache();

            newCacheIndex = _freeListHead;
            ref var newCacheEntry = ref _toleranceData[newCacheIndex];
            _freeListHead = newCacheEntry.RefCount;

            newCacheEntry.Values = cacheKey;
            newCacheEntry.RefCount = 1;
        }

        tileEntry = new TileData
        {
            BlockedDirections = blockedDirections,
            ToleranceCacheIndex = newCacheIndex,
        };
    }

    private void ExpandCache()
    {
        var newCacheSize = Math.Max(8, _toleranceData.Count * 2);
        var curSize = _toleranceData.Count;

        _toleranceData.EnsureLength(newCacheSize);
        for (var i = curSize; i < newCacheSize; i++)
        {
            _toleranceData[i].RefCount = _freeListHead;
            _freeListHead = i;
        }
    }

    private void DecrementRefCount(int index)
    {
        ref var cacheEntry = ref _toleranceData[index];

        DebugTools.Assert(cacheEntry.RefCount > 0);
        cacheEntry.RefCount -= 1;

        if (cacheEntry.RefCount == 0)
        {
            var prevValue = cacheEntry.Values;
            cacheEntry.Values = default;
            cacheEntry.RefCount = _freeListHead;
            _freeListHead = index;

            var result = _toleranceIndex.Remove(prevValue);
            DebugTools.Assert(result, "Failed to removed 0 refcounted index!");
        }
    }

    /// <summary>
    ///     On receiving damage, re-evaluate how much explosion damage is needed to destroy an airtight entity.
    /// </summary>
    private void OnAirtightDamaged(EntityUid uid, AirtightComponent airtight, DamageChangedEvent args)
    {
        // do we need to update our explosion blocking map?
        if (!airtight.AirBlocked)
            return;

        if (!TryComp(uid, out TransformComponent? transform) || !transform.Anchored)
            return;

        if (!TryComp<MapGridComponent>(transform.GridUid, out var grid))
            return;

        UpdateAirtightMap(transform.GridUid.Value, grid, _map.CoordinatesToTile(transform.GridUid.Value, grid, transform.Coordinates));
    }

    /// <summary>
    ///     Return a dictionary that specifies how intense a given explosion type needs to be in order to destroy an entity.
    /// </summary>
    private void GetExplosionTolerance(EntityUid uid, Span<FixedPoint2> explosionTolerance)
    {
        // How much total damage is needed to destroy this entity? This also includes "break" behaviors. This ASSUMES
        // that this will result in a non-airtight entity.Entities that ONLY break via construction graph node changes
        // are currently effectively "invincible" as far as this is concerned. This really should be done more rigorously.
        var totalDamageTarget = FixedPoint2.MaxValue;
        if (_destructibleQuery.TryGetComponent(uid, out var destructible))
        {
            totalDamageTarget = _destructibleSystem.DestroyedAt(uid, destructible);
        }

        if (totalDamageTarget == FixedPoint2.MaxValue || !_damageableQuery.TryGetComponent(uid, out var damageable))
        {
            for (var i = 0; i < explosionTolerance.Length; i++)
            {
                explosionTolerance[i] = ToleranceValues.Invulnerable;
            }

            return;
        }

        // What multiple of each explosion type damage set will result in the damage exceeding the required amount? This
        // does not support entities dynamically changing explosive resistances (e.g. via clothing). But these probably
        // shouldn't be airtight structures anyways....

        var mod = _damageableSystem.UniversalAllDamageModifier * _damageableSystem.UniversalExplosionDamageModifier;
        foreach (var (id, index) in _explosionTypes)
        {
            // TODO EXPLOSION SYSTEM
            // cache explosion type damage.
            if (!_prototypeManager.Resolve(id, out ExplosionPrototype? explosionType))
                continue;

            // evaluate the damage that this damage type would do to this entity
            var damagePerIntensity = FixedPoint2.Zero;
            foreach (var (type, value) in explosionType.DamagePerIntensity.DamageDict)
            {
                if (!damageable.Damage.DamageDict.ContainsKey(type))
                    continue;

                // TODO EXPLOSION SYSTEM
                // add a variant of the event that gets raised once, instead of once per prototype.
                // Or better yet, just calculate this manually w/o the event.
                // The event mainly exists for indirect resistances via things like inventory & clothing
                // But this shouldn't matter for airtight entities.
                var ev = new GetExplosionResistanceEvent(explosionType.ID);
                RaiseLocalEvent(uid, ref ev);

                damagePerIntensity += value * mod * Math.Max(0, ev.DamageCoefficient);
            }

            var toleranceValue = damagePerIntensity > 0
                ? (float) ((totalDamageTarget - damageable.TotalDamage) / damagePerIntensity)
                : ToleranceValues.Invulnerable;

            explosionTolerance[index] = toleranceValue;
        }
    }

    private void OnAirtightGridRemoved(EntityUid entity)
    {
        if (!TryComp(entity, out ExplosionAirtightGridComponent? airtightGrid))
            return;

        foreach (var tile in airtightGrid.Tiles.Values)
        {
            DecrementRefCount(tile.ToleranceCacheIndex);
        }

        RemComp<ExplosionAirtightGridComponent>(entity);
    }

    public override void ReloadMap()
    {
        var enumerator = EntityQueryEnumerator<ExplosionAirtightGridComponent, MapGridComponent>();
        while (enumerator.MoveNext(out var uid, out var airtightComp, out var mapGrid))
        {
            foreach (var pos in airtightComp.Tiles.Keys)
            {
                UpdateAirtightMap(uid, pos, mapGrid);
            }
        }
    }

    private struct CacheEntry
    {
        public ToleranceValues Values;
        public int RefCount; // Doubles as freelist chain
    }

}
