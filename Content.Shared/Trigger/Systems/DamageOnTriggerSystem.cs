using Content.Shared.Damage;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class DamageOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<DamageOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        var damage = new DamageSpecifier(ent.Comp.Damage);
        var ev = new BeforeDamageOnTriggerEvent(damage, target.Value);
        RaiseLocalEvent(ent.Owner, ref ev);

        args.Handled |= _damageableSystem.TryChangeDamage(target, ev.Damage, ent.Comp.IgnoreResistances, origin: ent.Owner) is not null;
    }
}

/// <summary>
/// Raised on an entity before it deals damage using DamageOnTriggerComponent.
/// Used to modify the damage that will be dealt.
/// </summary>
[ByRefEvent]
public record struct BeforeDamageOnTriggerEvent(DamageSpecifier Damage, EntityUid Tripper);
