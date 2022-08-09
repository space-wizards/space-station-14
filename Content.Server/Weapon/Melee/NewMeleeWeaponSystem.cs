using Content.Shared.Weapon.Melee;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Player;

namespace Content.Server.Weapon.Melee;

public sealed class NewMeleeWeaponSystem : SharedNewMeleeWeaponSystem
{
    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (uid == null)
            return;

        PopupSystem.PopupEntity(message, uid.Value, Filter.Pvs(uid.Value, entityManager: EntityManager).RemoveWhereAttachedEntity(e => e == user));
    }
}
