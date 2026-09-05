using Content.Shared.GhostTypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Body;

/// <summary>
/// Sets a death cause to the entity
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class SpecialDeathCauseEffectSystem : EntityEffectSystem<StoreDamageTakenOnMindComponent, SpecialDeathCause>
{
    [Dependency] private StoreDamageTakenOnMindSystem _storeSystem = default!;
    protected override void Effect(Entity<StoreDamageTakenOnMindComponent> entity, ref EntityEffectEvent<SpecialDeathCause> args)
    {
        _storeSystem.SaveSpecialCauseOfDeath(entity, args.Effect.DeathCause);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class SpecialDeathCause : EntityEffectBase<SpecialDeathCause>
{
    /// <summary>
    /// The special death cause that will be added to the entity affected by this entity effect
    /// </summary>
    [DataField(required: true)]
    public ProtoId<SpecialCauseOfDeathPrototype> DeathCause;
}
