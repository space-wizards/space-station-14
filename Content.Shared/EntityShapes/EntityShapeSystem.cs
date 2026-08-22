using Content.Shared.EntityShapes.Components;
using Content.Shared.EntityShapes.Shapes;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.EntityShapes;

public sealed partial class EntityShapeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;

    [Dependency] private EntityQuery<ShapeSpawnerComponent> _spawnerQuery = default!;
    [Dependency] private EntityQuery<ShapeSpawnerCounterComponent> _counterQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShapeSpawnerComponent, MapInitEvent>(OnSpawnerInit);
        SubscribeLocalEvent<ShapeSpawnerCounterComponent, MapInitEvent>(OnCounterInit);
        SubscribeLocalEvent<ExpandingShapeSpawnerComponent, SpawnCounterEntityShapeEvent>(OnExpandingShapeTrigger);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ShapeSpawnerComponent, ShapeSpawnerCounterComponent>();
        while (query.MoveNext(out var uid, out var spawnerComp, out var counterComp))
        {
            if (counterComp.NextSpawn > curTime)
                continue;

            if (counterComp.Counter == counterComp.MaxCounter)
            {
                if (spawnerComp.DeleteSpawnerAfterSpawn)
                    PredictedQueueDel(uid);

                continue;
            }

            counterComp.NextSpawn = curTime + counterComp.SpawnPeriod;
            counterComp.Counter++;

            var ev = new SpawnCounterEntityShapeEvent(counterComp.Counter);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    /// <inheritdoc cref="SpawnEntityShape(EntityShape?, EntityCoordinates, EntProtoId, out List{EntityUid})"/>
    [PublicAPI]
    public void SpawnEntityShape(EntityShape shape, EntityUid target, EntProtoId spawnId, out List<EntityUid> spawned, bool alignTile = false)
    {
        var coords = alignTile
            ? Transform(target).Coordinates.AlignWithClosestGridTile(1.5f, EntityManager)
            : Transform(target).Coordinates;

        SpawnEntityShape(shape, coords, spawnId, out spawned);
    }

    /// <summary>
    /// Calculates all positions of an <see cref="EntityShape"/> and spawns <see cref="EntProtoId"/> on them.
    /// </summary>
    /// <param name="shape">The shape to calculate the positions.</param>
    /// <param name="coords">Coordinates of the center of the shape.</param>
    /// <param name="spawnId">A proto ID of the entities.</param>
    /// <param name="spawned">List of all spawned entities.</param>
    /// <remarks>
    /// Use this only if you need to get all spawned entities by this shape,
    /// otherwise it's better to spawn an entity with ShapeSpawnerComponent,
    /// since it allows for more functionality.
    /// </remarks>
    [PublicAPI]
    public void SpawnEntityShape(EntityShape? shape, EntityCoordinates coords, EntProtoId spawnId, out List<EntityUid> spawned)
    {
        spawned = new List<EntityUid>();

        // Prevents the spawn menu from exploding
        if (!coords.IsValid(EntityManager))
            return;

        if (shape == null)
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(coords.EntityId));
        var result = GetShape(shape, coords.Position, random);
        foreach (var pos in result)
        {
            var coord = new EntityCoordinates(coords.EntityId, pos);
            var ent = PredictedSpawnAtPosition(spawnId, coord);
            spawned.Add(ent);
        }
    }

    /// <inheritdoc cref="SpawnEntityShape(EntityShape?, EntityCoordinates, EntityTableSelector, out List{EntityUid})"/>
    [PublicAPI]
    public void SpawnEntityShape(EntityShape shape, EntityUid target, EntityTableSelector table, out List<EntityUid> spawned, bool alignTile = false)
    {
        var coords = alignTile
            ? Transform(target).Coordinates.AlignWithClosestGridTile(1.5f, EntityManager)
            : Transform(target).Coordinates;

        SpawnEntityShape(shape, coords, table, out spawned);
    }

    /// <summary>
    /// Calculates all positions of an <see cref="EntityShape"/> and spawns <see cref="EntProtoId"/> on them.
    /// </summary>
    /// <param name="shape">The shape to calculate the positions.</param>
    /// <param name="coords">Coordinates of the center of the shape.</param>
    /// <param name="table">The table to spawn at each point.</param>
    /// <param name="spawned">List of all spawned entities.</param>
    /// <remarks>
    /// Use this only if you need to get all spawned entities by this shape,
    /// otherwise it's better to spawn an entity with ShapeSpawnerComponent,
    /// since it allows for more functionality.
    /// </remarks>
    [PublicAPI]
    public void SpawnEntityShape(EntityShape? shape, EntityCoordinates coords, EntityTableSelector table, out List<EntityUid> spawned)
    {
        spawned = new List<EntityUid>();

        // Prevents the spawn menu from exploding
        if (!coords.IsValid(EntityManager))
            return;

        if (shape == null)
            return;

        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(coords.EntityId));
        var result = GetShape(shape, coords.Position, random);
        foreach (var pos in result)
        {
            var coord = new EntityCoordinates(coords.EntityId, pos);
            var spawns = _entityTable.GetSpawns(table, random);
            foreach (var spawn in spawns)
            {
                var ent = PredictedSpawnAtPosition(spawn, coord);
                spawned.Add(ent);
            }
        }
    }

    private void OnSpawnerInit(Entity<ShapeSpawnerComponent> ent, ref MapInitEvent args)
    {
        SpawnEntityShape(ent.Comp.Shape, ent.Owner, ent.Comp.Spawn, out _, ent.Comp.AlignCoords);

        if (!_counterQuery.HasComp(ent.Owner) // Deletion handled after the spawner loop is done
            && ent.Comp.DeleteSpawnerAfterSpawn)
            PredictedQueueDel(ent.Owner);
    }

    private void OnCounterInit(Entity<ShapeSpawnerCounterComponent> ent, ref MapInitEvent args)
    {
        // First shape is spawned by an event anyway, so delay the counter one to be later
        ent.Comp.NextSpawn = _timing.CurTime + ent.Comp.SpawnPeriod;
    }

    private void OnExpandingShapeTrigger(Entity<ExpandingShapeSpawnerComponent> ent, ref SpawnCounterEntityShapeEvent args)
    {
        var (uid, comp) = ent;

        if (!_spawnerQuery.TryComp(uid, out var spawner))
            return;

        if (comp.CounterOffset != null)
            spawner.Shape.DefaultOffset = comp.CounterOffset.Value * args.Counter;

        if (comp.CounterSize != null)
            spawner.Shape.DefaultSize = (int) Math.Round(comp.CounterSize.Value * args.Counter);

        if (comp.CounterStepSize != null)
            spawner.Shape.DefaultStepSize = (int) Math.Round(comp.CounterStepSize.Value * args.Counter);

        SpawnEntityShape(spawner.Shape, uid, spawner.Spawn, out _, spawner.AlignCoords);
    }
}
