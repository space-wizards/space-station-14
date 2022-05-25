using Content.Server.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.StepTrigger;

namespace Content.Server.Damage.Systems;

// System for damage that occurs on specific triggers.
// This is originally meant for mousetraps, but could
// probably be extended to fit other triggers as well
// if they're ever coded.
public sealed class DamageOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageOnTriggerComponent, StepTriggeredEvent>(OnStepTrigger);
    }

    public void OnStepTrigger(EntityUid uid, DamageOnTriggerComponent component, ref StepTriggeredEvent args)
    {
        OnDamageTrigger(uid, args.Tripper, component);
    }

    public void OnDamageTrigger(EntityUid source, EntityUid target, DamageOnTriggerComponent? component = null)
    {
        if (!Resolve(source, ref component))
        {
            return;
        }

        _damageableSystem.TryChangeDamage(target, component.Damage, component.IgnoreResistances);
    }
}
