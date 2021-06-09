using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    // TODO: Remove this, just use MeleeWeapon...
    [RegisterComponent]
    [ComponentReference(typeof(MeleeWeaponComponent))]
    public class UnarmedCombatComponent : MeleeWeaponComponent
    {
        public override string Name => "UnarmedCombat";
    }
}
