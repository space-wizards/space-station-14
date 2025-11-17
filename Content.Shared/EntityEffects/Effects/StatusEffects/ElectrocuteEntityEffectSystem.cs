using Content.Shared.Electrocution;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

// TODO: When Electrocution is moved to new Status, make this use StatusEffectsContainerComponent.
/// <summary>
/// Electrocutes this entity for a given amount of damage and time.
/// The shock damage applied by this effect is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ElectrocuteEntityEffectSystem : EntityEffectSystem<StatusEffectsComponent, Electrocute>
{
    [Dependency] private readonly SharedElectrocutionSystem _electrocution = default!;

    // TODO: When electrocution is new status, change this to new status
    protected override void Effect(Entity<StatusEffectsComponent> entity, ref EntityEffectEvent<Electrocute> args)
    {
        var effect = args.Effect;

        _electrocution.TryDoElectrocution(entity, null, (int)(args.Scale * effect.ShockDamage), effect.ElectrocuteTime, effect.Refresh, ignoreInsulation: effect.BypassInsulation);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class Electrocute : EntityEffectBase<Electrocute>
{
    /// <summary>
    /// Time we electrocute this entity
    /// </summary>
    [DataField] public TimeSpan ElectrocuteTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Shock damage we apply to the entity.
    /// </summary>
    [DataField] public int ShockDamage = 5;

    /// <summary>
    /// Do we refresh the duration? Or add more duration if it already exists.
    /// </summary>
    [DataField] public bool Refresh = true;

    /// <summary>
    /// Should we by bypassing insulation?
    /// </summary>
    [DataField] public bool BypassInsulation = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-electrocute", ("chance", Probability), ("time", ElectrocuteTime.TotalSeconds));
}
