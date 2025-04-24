using Content.Server.Atmos.Components;
using Content.Server.Destructible;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Map.Components;

namespace Content.Server.Explosion.EntitySystems;

public sealed partial class ExplosionSystem
{
    [Dependency] private readonly DestructibleSystem _destructibleSystem = default!;

    private readonly Dictionary<string, int> _explosionTypes = new();

    private void InitAirtightMap()
    {
        // Currently explosion prototype hot-reload isn't supported, as it would involve completely re-computing the
        // airtight map. Could be done, just not yet implemented.

        // for storing airtight entity damage thresholds for all anchored airtight entities, we will use integers in
        // place of id-strings. This initializes the string <--> id association.
        // This allows us to replace a Dictionary<string, float> with just a float[].
        int index = 0;
        foreach (var prototype in _prototypeManager.EnumeratePrototypes<ExplosionPrototype>())
        {
            _explosionTypes.Add(prototype.ID, index);
            index++;
        }
    }

    // The explosion intensity required to break an entity depends on the explosion type. So it is stored in a
    // Dictionary<string, float>
    //
    // Hence, each tile has a tuple (Dictionary<string, float>, AtmosDirection). This specifies what directions are
    // blocked, and how intense a given explosion type needs to be in order to destroy ALL airtight entities on that
    // tile. This is the TileData struct.
    //
    // We then need this data for every tile on a grid. So this mess of a variable maps the Grid ID and Vector2i grid
    // indices to this tile-data struct.
    private Dictionary<EntityUid, Dictionary<Vector2i, TileData>> _airtightMap = new();

    public void UpdateAirtightMap(EntityUid gridId, Vector2i tile, MapGridComponent? grid = null, EntityQuery<AirtightComponent>? query = null)
    {
        if (Resolve(gridId, ref grid, false))
            UpdateAirtightMap(gridId, grid, tile, query);
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
    public void UpdateAirtightMap(EntityUid gridId, MapGridComponent grid, Vector2i tile, EntityQuery<AirtightComponent>? query = null)
    {
        var tolerance = new float[_explosionTypes.Count];
        var blockedDirections = AtmosDirection.Invalid;

        if (!_airtightMap.ContainsKey(gridId))
            _airtightMap[gridId] = new();

        query ??= EntityManager.GetEntityQuery<AirtightComponent>();
        var damageQuery = EntityManager.GetEntityQuery<DamageableComponent>();
        var destructibleQuery = EntityManager.GetEntityQuery<DestructibleComponent>();
        var anchoredEnumerator = _mapSystem.GetAnchoredEntitiesEnumerator(gridId, grid, tile);

        while (anchoredEnumerator.MoveNext(out var uid))
        {
            if (!query.Value.TryGetComponent(uid, out var airtight) || !airtight.AirBlocked)
                continue;

            blockedDirections |= airtight.AirBlockedDirection;
            var entityTolerances = GetExplosionTolerance(uid.Value, damageQuery, destructibleQuery);
            for (var i = 0; i < tolerance.Length; i++)
            {
                tolerance[i] = Math.Max(tolerance[i], entityTolerances[i]);
            }
        }

        if (blockedDirections != AtmosDirection.Invalid)
            _airtightMap[gridId][tile] = new(tolerance, blockedDirections);
        else
            _airtightMap[gridId].Remove(tile);
    }

    /// <summary>
    ///     On receiving damage, re-evaluate how much explosion damage is needed to destroy an airtight entity.
    /// </summary>
    private void OnAirtightDamaged(EntityUid uid, AirtightComponent airtight, DamageChangedEvent args)
    {
        // do we need to update our explosion blocking map?
        if (!airtight.AirBlocked)
            return;

        if (!EntityManager.TryGetComponent(uid, out TransformComponent? transform) || !transform.Anchored)
            return;

        if (!TryComp<MapGridComponent>(transform.GridUid, out var grid))
            return;

        UpdateAirtightMap(transform.GridUid.Value, grid, _mapSystem.CoordinatesToTile(transform.GridUid.Value, grid, transform.Coordinates));
    }

    /// <summary>
    ///     Return a dictionary that specifies how intense a given explosion type needs to be in order to destroy an entity.
    /// </summary>
    public float[] GetExplosionTolerance(
        EntityUid uid,
        EntityQuery<DamageableComponent> damageQuery,
        EntityQuery<DestructibleComponent> destructibleQuery)
    {
        // How much total damage is needed to destroy this entity? This also includes "break" behaviors. This ASSUMES
        // that this will result in a non-airtight entity.Entities that ONLY break via construction graph node changes
        // are currently effectively "invincible" as far as this is concerned. This really should be done more rigorously.
        var totalDamageTarget = FixedPoint2.MaxValue;
        if (destructibleQuery.TryGetComponent(uid, out var destructible))
        {
            totalDamageTarget = _destructibleSystem.DestroyedAt(uid, destructible);
        }

        var explosionTolerance = new float[_explosionTypes.Count];
        if (totalDamageTarget == FixedPoint2.MaxValue || !damageQuery.TryGetComponent(uid, out var damageable))
        {
            for (var i = 0; i < explosionTolerance.Length; i++)
            {
                explosionTolerance[i] = float.MaxValue;
            }
            return explosionTolerance;
        }

        // What multiple of each explosion type damage set will result in the damage exceeding the required amount? This
        // does not support entities dynamically changing explosive resistances (e.g. via clothing). But these probably
        // shouldn't be airtight structures anyways....

        foreach (var (id, index) in _explosionTypes)
        {
            if (!_prototypeManager.TryIndex<ExplosionPrototype>(id, out var explosionType))
                continue;

            // evaluate the damage that this damage type would do to this entity
            var damagePerIntensity = FixedPoint2.Zero;
            foreach (var (type, value) in explosionType.DamagePerIntensity.DamageDict)
            {
                if (!damageable.Damage.DamageDict.ContainsKey(type))
                    continue;

                var ev = new GetExplosionResistanceEvent(explosionType.ID);
                RaiseLocalEvent(uid, ref ev);

                damagePerIntensity += value * Math.Max(0, ev.DamageCoefficient);
            }

            explosionTolerance[index] = damagePerIntensity > 0
                ? (float) ((totalDamageTarget - damageable.TotalDamage) / damagePerIntensity)
                : float.MaxValue;
        }

        return explosionTolerance;
    }

    /// <summary>
    ///     Data struct that describes the explosion-blocking airtight entities on a tile.
    /// </summary>
    public struct TileData
    {
        public TileData(float[] explosionTolerance, AtmosDirection blockedDirections)
        {
            ExplosionTolerance = explosionTolerance;
            BlockedDirections = blockedDirections;
        }

        public float[] ExplosionTolerance;
        public AtmosDirection BlockedDirections = AtmosDirection.Invalid;
    }
}
