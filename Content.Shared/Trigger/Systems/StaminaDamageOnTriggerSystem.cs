using Content.Shared.Damage.Systems;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class StaminaDamageOnTriggerSystem : XOnTriggerSystem<StaminaDamageOnTriggerComponent>
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    protected override void OnTrigger(Entity<StaminaDamageOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var ev = new BeforeStaminaDamageOnTriggerEvent(ent.Comp.Stamina, target);
        RaiseLocalEvent(ent.Owner, ref ev);

        _stamina.TakeStaminaDamage(target, ev.Stamina, source: args.User, with: ent.Owner, ignoreResist: ent.Comp.IgnoreResistances);

        args.Handled = true;
    }
}

/// <summary>
/// Raised on an entity before it inflicts stamina due to StaminaDamageOnTriggerComponent.
/// Used to modify the stamina that will be inflicted.
/// </summary>
[ByRefEvent]
public record struct BeforeStaminaDamageOnTriggerEvent(float Stamina, EntityUid Tripper);
