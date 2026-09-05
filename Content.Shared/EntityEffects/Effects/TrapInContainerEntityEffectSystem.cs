using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Causes this entity to glow.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class TrapInContainerEntityEffectSystem : EntityEffectSystem<TransformComponent, TrapInContainer>
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private INetManager _net = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<TrapInContainer> args)
    {
        // PredictedSpawn together with inserting things into containers is a bad combo
        // I don't know how to predict this without Predicted Spawning v2.0
        // This is because currently PredictedSpawn spawns and bulldozes clientside entities on every prediction tick
        // and this causes client to crap itself when receiving server states for an entity that no longer exists if mixed with container insertion etc.
        // God save me.
        if (!_net.IsServer)
            return;

        var containerEntity = SpawnAtPosition(args.Effect.Entity, entity.Comp.Coordinates);

        if (!_container.TryGetContainer(containerEntity, args.Effect.Container, out var container))
        {
            QueueDel(containerEntity);
            return;
        }

        if (!_container.Insert(entity.Owner, container, force: true))
            QueueDel(containerEntity);
    }
}

public sealed partial class TrapInContainer : EntityEffectBase<TrapInContainer>
{
    /// <summary>
    /// The container entity to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Entity;

    /// <summary>
    /// What container the entity should be trapped in.
    /// </summary>
    [DataField]
    public string Container = "entity_storage";

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-make-polymorph",
        ("chance", Probability),
        ("entityname", prototype.Index<EntityPrototype>(prototype.Index(Entity).Name)));
}
