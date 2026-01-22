using Content.Shared.Destructible;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.MetaData;


/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DestructibleActEntityEffectSystem : EntityEffectSystem<MetaDataComponent, DestructibleAct>
{
    [Dependency] private readonly SharedDestructibleSystem _destructible = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<DestructibleAct> args)
    {
        if ((args.Effect.Acts & ThresholdActs.Breakage) != 0)
            _destructible.BreakEntity(entity);

        if ((args.Effect.Acts & ThresholdActs.Destruction) != 0)
            _destructible.DestroyEntity(entity.AsNullable());
    }
}

/// <summary>
/// Destroys or breaks an entity.
/// </summary>
public sealed partial class DestructibleAct : EntityEffectBase<DestructibleAct>
{
    /// <summary>
    /// What acts should be triggered upon activation.
    /// </summary>
    [DataField]
    public ThresholdActs Acts;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if ((Acts & ThresholdActs.Destruction) != 0)
            return Loc.GetString("entity-effect-guidebook-destroy", ("chance", Probability));

        return Loc.GetString("entity-effect-guidebook-break", ("chance", Probability));
    }
}
