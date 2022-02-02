using Robust.Shared.GameObjects;

namespace Content.Server.Weapon.Melee.Components
{
    // TODO: Remove this, just use MeleeWeapon...
    [RegisterComponent]
    [ComponentReference(typeof(MeleeWeaponComponent))]
    public class UnarmedCombatComponent : MeleeWeaponComponent
    {
    }
}
