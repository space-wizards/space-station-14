using Content.Server.Explosion.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;

namespace Content.Server.Damage.Systems;

/// <summary>
/// System for damage that occurs on specific trigger events, towards the user...
/// </summary>
/// <remarks>
/// The <see cref="TriggerEvent"/> must have <see cref="TriggerEvent.User"/> argument.
/// The user must have a <see cref="DamageableComponent"/> (e.g. being a creature or player) to receive damage.
/// </remarks>
public sealed class DamageUserOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageUserOnTriggerComponent, TriggerEvent>(OnUserTrigger);
    }

    private void OnUserTrigger(Entity<DamageUserOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.User is null)
            return;

        var comp = ent.Comp;

        args.Handled |= TryDamageTarget(ent.Owner, comp.Damage, comp.IgnoreResistances, args.User.Value);
    }

    private bool TryDamageTarget(EntityUid uid, DamageSpecifier damage, bool ignoreResistances, EntityUid target)
    {
        var ev = new BeforeDamageUserOnTriggerEvent(damage, target);
        RaiseLocalEvent(uid, ev);

        return _damageableSystem.TryChangeDamage(target, ev.Damage, ignoreResistances, origin: uid) is not null;
    }
}

/// <summary>
/// Raised before applying damage to the user that triggered a DamageUserOnTrigger component.
/// Allows other systems to modify the damage.
/// </summary>
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
