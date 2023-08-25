using Content.Server.Damage.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.StepTrigger;
using Content.Shared.StepTrigger.Systems;

namespace Content.Server.Damage.Systems;

// System for damage that occurs on specific trigger, towards the user..
public sealed class DamageUserOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageUserOnTriggerComponent, TriggerEvent>(OnTrigger);
        SubscribeLocalEvent<DamageTriggerOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(EntityUid uid, DamageUserOnTriggerComponent component, TriggerEvent args)
    {
        if (args.User is null)
            return;

        args.Handled |= OnDamageTripper(uid, args.User.Value, component);
    }

    private void OnTrigger(EntityUid uid, DamageTriggerOnTriggerComponent component, TriggerEvent args)
    {
        args.Handled |= OnDamageTrigger(uid, component);
    }

    private bool OnDamageTripper(EntityUid source, EntityUid target, DamageUserOnTriggerComponent? component = null)
    {
        if (!Resolve(source, ref component))
        {
            return false;
        }

        var damage = new DamageSpecifier(component.Damage);
        var ev = new BeforeDamageUserOnTriggerEvent(damage, target);
        RaiseLocalEvent(source, ev);

        return _damageableSystem.TryChangeDamage(target, ev.Damage, component.IgnoreResistances, origin: source) != null;
    }

    private bool OnDamageTrigger(EntityUid uid, DamageTriggerOnTriggerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
        {
            return false;
        }

        var damage = new DamageSpecifier(comp.Damage);
        var ev = new BeforeDamageTriggerOnTriggerEvent(damage);
        RaiseLocalEvent(uid, ev);

        return _damageableSystem.TryChangeDamage(uid, ev.Damage, comp.IgnoreResistances, origin: uid) != null;
    }
}

public sealed class BeforeDamageUserOnTriggerEvent : EntityEventArgs
{
    public DamageSpecifier Damage { get; set; }
    public EntityUid Tripper { get; }

    public BeforeDamageUserOnTriggerEvent(DamageSpecifier damage, EntityUid target)
    {
        Damage = damage;
        Tripper = target;
    }
}

public sealed class BeforeDamageTriggerOnTriggerEvent : EntityEventArgs
{
    public DamageSpecifier Damage { get; set; }

    public BeforeDamageTriggerOnTriggerEvent(DamageSpecifier damage)
    {
        Damage = damage;
    }
}
