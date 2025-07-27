using Content.Server.Explosion.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;

namespace Content.Server.Damage.Systems;

/// <summary>
/// Applies damage to the entity that has the <see cref="TriggerEvent"/>.
/// </summary>
/// <remarks>
/// The triggering entity must have a <see cref="DamageableComponent"/> (e.g. being a creature or player) to receive damage.
/// Unlike <see cref="DamageUserOnTriggerSystem"/>, this applies damage directly to the triggering entity itself,
/// not to the user.
/// </remarks>
public sealed class DamageOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DamageOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<DamageOnTriggerComponent> ent, ref TriggerEvent args)
    {
        var comp = ent.Comp;

        args.Handled |= TryDamageTarget(ent.Owner, comp.Damage, comp.IgnoreResistances, ent.Owner);
    }

    private bool TryDamageTarget(EntityUid uid, DamageSpecifier damage, bool ignoreResistances, EntityUid target)
    {
        var ev = new BeforeDamageOnTriggerEvent(damage, target);
        RaiseLocalEvent(uid, ev);

        return _damageableSystem.TryChangeDamage(target, ev.Damage, ignoreResistances, origin: uid) is not null;
    }
}

/// <summary>
/// Raised before applying damage to the triggered entity itself (i.e., the one with the <see cref="DamageOnTriggerComponent"/>).
/// Allows other systems to modify the damage.
/// </summary>
public sealed class BeforeDamageOnTriggerEvent : EntityEventArgs
{
    public DamageSpecifier Damage { get; set; }
    public EntityUid Tripper { get; }

    public BeforeDamageOnTriggerEvent(DamageSpecifier damage, EntityUid target)
    {
        Damage = damage;
        Tripper = target;
    }
}
