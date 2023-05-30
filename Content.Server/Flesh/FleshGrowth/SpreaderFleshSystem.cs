using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Server.Destructible;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;

namespace Content.Server.Flesh.FleshGrowth;

// Future work includes making the growths per interval thing not global, but instead per "group"
public sealed class SpreaderFleshSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    /// <summary>
    /// Maximum number of edges that can grow out every interval.
    /// </summary>
    private const int GrowthsPerInterval = 6;

    private float _accumulatedFrameTime = 0.0f;

    private readonly HashSet<EntityUid> _edgeGrowths = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<SpreaderFleshComponent, ComponentAdd>(SpreaderAddHandler);
        SubscribeLocalEvent<AirtightChanged>(OnAirtightChanged);
    }

    private void OnAirtightChanged(ref AirtightChanged ev)
    {
        UpdateNearbySpreaders(ev.Entity, ev.Airtight);
    }

    private void SpreaderAddHandler(EntityUid uid, SpreaderFleshComponent component, ComponentAdd args)
    {
        if (component.Enabled)
            _edgeGrowths.Add(uid); // ez
    }

    public void UpdateNearbySpreaders(EntityUid blocker, AirtightComponent comp)
    {
        if (!EntityManager.TryGetComponent<TransformComponent>(blocker, out var transform))
            return; // how did we get here?

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid))
            return;

        var spreaderQuery = GetEntityQuery<FleshGrowth.SpreaderFleshComponent>();
        var tile = grid.TileIndicesFor(transform.Coordinates);

        for (var i = 0; i < Atmospherics.Directions; i++)
        {
            var direction = (AtmosDirection) (1 << i);
            if (!comp.AirBlockedDirection.IsFlagSet(direction))
                continue;

            var directionEnumerator =
                grid.GetAnchoredEntitiesEnumerator(SharedMapSystem.GetDirection(tile, direction.ToDirection()));

            while (directionEnumerator.MoveNext(out var ent))
            {
                if (spreaderQuery.TryGetComponent(ent, out var s) && s.Enabled)
                    _edgeGrowths.Add(ent.Value);
            }
        }
    }

    public override void Update(float frameTime)
    {
        _accumulatedFrameTime += frameTime;

        if (!(_accumulatedFrameTime >= 1.0f))
            return;

        _accumulatedFrameTime -= 1.0f;

        var growthList = _edgeGrowths.ToList();
        _robustRandom.Shuffle(growthList);

        var successes = 0;
        foreach (var entity in growthList)
        {
            if (!TryGrow(entity))
                continue;

            successes += 1;
            if (successes >= GrowthsPerInterval)
                break;
        }
    }

    private bool TryGrow(EntityUid ent, TransformComponent? transform = null, SpreaderFleshComponent? spreader = null)
    {
        if (!Resolve(ent, ref transform, ref spreader, false))
            return false;

        if (spreader.Enabled == false)
            return false;

        if (!_mapManager.TryGetGrid(transform.GridUid, out var grid))
            return false;

        var didGrow = false;

        for (var i = 0; i < 4; i++)
        {
            var direction = (DirectionFlag) (1 << i);
            var coords = transform.Coordinates.Offset(direction.AsDir().ToVec());
            if (grid.GetTileRef(coords).Tile.IsEmpty || _robustRandom.Prob(1 - spreader.Chance))
                continue;
            var ents = grid.GetLocal(coords);

            var entityUids = ents as EntityUid[] ?? ents.ToArray();
            if (entityUids.Any(x => IsTileBlockedFrom(x, direction)))
                continue;

            var canSpawnWall = true;
            var canSpawnFloor = true;
            string entityStrucrureId = String.Empty;
            foreach (var entityUid in entityUids)
            {
                if (_tagSystem.HasAnyTag(entityUid, "Wall", "Window"))
                {
                    if (!_tagSystem.HasAnyTag(entityUid, "Directional"))
                    {
                        if (TryComp(entityUid, out MetaDataComponent? metaData))
                        {
                            if (metaData.EntityPrototype != null)
                                entityStrucrureId = metaData.EntityPrototype.ID;
                        }

                        canSpawnFloor = false;
                    }
                }

                if (_tagSystem.HasAnyTag(entityUid, "Flesh", "Directional"))
                {
                    canSpawnWall = false;
                }
            }

            if (canSpawnFloor)
            {
                didGrow = true;
                EntityManager.SpawnEntity(spreader.GrowthResult,
                    transform.Coordinates.Offset(direction.AsDir().ToVec()));
            }
            else
            {
                if (!canSpawnWall)
                    continue;
                didGrow = true;
                var uid = EntityManager.SpawnEntity(spreader.WallResult,
                    transform.Coordinates.Offset(direction.AsDir().ToVec()));
                if (EntityManager.TryGetComponent(uid, out DestructibleComponent? destructible))
                {
                    destructible.Thresholds.Clear();
                    var damageThreshold = new DamageThreshold
                    {
                        Trigger = new DamageTrigger { Damage = 5 }
                    };
                    damageThreshold.AddBehavior(new SpawnEntitiesBehavior
                    {
                        Spawn = new Dictionary<string, MinMax> { { entityStrucrureId, new MinMax{Min = 1, Max = 1} } },
                        Offset = 0f
                    });
                    damageThreshold.AddBehavior(new DoActsBehavior
                    {
                        Acts = ThresholdActs.Destruction
                    });
                    destructible.Thresholds.Add(damageThreshold);
                }

                foreach (var entityUid in entityUids)
                {
                    if (_tagSystem.HasAnyTag(entityUid, "Wall", "Window"))
                        EntityManager.DeleteEntity(entityUid);
                }
            }
        }

        return didGrow;
    }

    private bool IsTileBlockedFrom(EntityUid ent, DirectionFlag dir)
    {
        if (EntityManager.TryGetComponent<SpreaderFleshComponent>(ent, out _))
            return true;

        if (EntityManager.TryGetComponent<AirtightComponent>(ent, out var airtight))
            return true;

        return false;
    }
}
