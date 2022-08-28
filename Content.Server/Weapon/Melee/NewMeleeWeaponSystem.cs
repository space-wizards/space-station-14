using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Weapon.Melee;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Melee;

public sealed class NewMeleeWeaponSystem : SharedNewMeleeWeaponSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (uid == null)
            return;

        PopupSystem.PopupEntity(message, uid.Value, Filter.Pvs(uid.Value, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == user));
    }

    protected override void DoPreciseAttack(EntityUid user, ReleasePreciseAttackEvent ev, NewMeleeWeaponComponent component)
    {
        base.DoPreciseAttack(user, ev, component);

        if (!Deleted(ev.Target) &&
            _interaction.InRangeUnobstructed(user, ev.Target))
        {
            // TODO: Copy existing melee code
            var damage = _damageable.TryChangeDamage(ev.Target, component.Damage);

            if (damage?.Total != FixedPoint2.Zero)
            {
                // Damage
                Audio.PlayPvs(component.DamageSound, ev.Target);
            }
            else
            {
                // No damage
                Audio.PlayPvs(component.NoDamageSound, ev.Target);
            }
        }
    }
}
