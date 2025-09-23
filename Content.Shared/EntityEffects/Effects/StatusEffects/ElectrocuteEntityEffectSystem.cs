using Content.Shared.Electrocution;
using Content.Shared.StatusEffect;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

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
}
