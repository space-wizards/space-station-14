using Content.Server.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.StepTrigger;

namespace Content.Server.Damage.Systems;

// System for damage that occurs on specific triggers.
// This is originally meant for mousetraps, but could
// probably be extended to fit other triggers as well.
public sealed class DamageOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageOnTriggerComponent, StepTriggeredEvent>(OnStepTrigger);
    }

    private void OnStepTrigger(EntityUid uid, DamageOnTriggerComponent component, ref StepTriggeredEvent args)
    {
        OnDamageTrigger(uid, args.Tripper, component);
    }

    private void OnDamageTrigger(EntityUid source, EntityUid target, DamageOnTriggerComponent? component = null)
    {
        if (!Resolve(source, ref component))
        {
            return;
        }

        var damage = new DamageSpecifier(component.Damage);
        var ev = new BeforeDamageOnTriggerEvent(damage, target);
        RaiseLocalEvent(source, ev, true);

        _damageableSystem.TryChangeDamage(target, ev.Damage, component.IgnoreResistances);
    }
}

public sealed class BeforeDamageOnTriggerEvent : EntityEventArgs
{
    public DamageSpecifier Damage { get; set;  }
    public EntityUid Tripper { get; }

    public BeforeDamageOnTriggerEvent(DamageSpecifier damage, EntityUid target)
    {
        Damage = damage;
        Tripper = target;
    }
}
