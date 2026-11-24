using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Localizations;
using Content.Shared.Random.Helpers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Spawns a set of entities supplied by an <see cref="EntityTableSelector"/> next to the entity this is applied to.
/// Scaled only by integer amounts.
/// </summary>
public sealed partial class SpawnEntityTableEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnEntityTable>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnEntityTable> args)
    {
        var amount = (int)Math.Floor(args.Scale);
        if (amount <= 0)
            return;

        var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));
        var spawns = new List<EntProtoId>(amount); // Assumption that every table has at least one thing to select
        for (var i = 0; i < amount; i++)
        {
            spawns.AddRange(_entityTable.GetSpawns(args.Effect.Table, rand));
        }

        if (args.Effect.Predicted)
        {
            foreach (var spawn in spawns)
            {
                PredictedSpawnNextToOrDrop(spawn, entity, entity.Comp);
            }
        }
        else if (_net.IsServer)
        {
            foreach (var spawn in spawns)
            {
                SpawnNextToOrDrop(spawn, entity, entity.Comp);
            }
        }
    }
}

/// <summary>
/// A type of <see cref="EntityEffectBase{T}"/> that spawns from an entity table.
/// </summary>
public sealed partial class SpawnEntityTable : EntityEffectBase<SpawnEntityTable>
{
    private EntityTableSystem? _entityTable;

    /// <summary>
    /// The table being spawned.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table;

    /// <summary>
    /// Whether this spawning is predicted.
    /// </summary>
    [DataField]
    public bool Predicted = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // Get it once and keep it for later
        _entityTable ??= entSys.GetEntitySystem<EntityTableSystem>();

        var spawnNames = new List<string>();
        foreach (var (entity, _) in _entityTable.ListSpawns(Table))
        {
            spawnNames.Add(prototype.Index(entity).Name);
        }

        return Loc.GetString("entity-effect-guidebook-spawn-entity-table",
                            ("chance", Probability),
                            ("names", ContentLocalizationManager.FormatListToAndOr(spawnNames)));
    }
}
