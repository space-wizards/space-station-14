using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Weapon.Melee
{
    [RegisterComponent]
    public class UnarmedCombatComponent : MeleeWeaponComponent
    {
        public override string Name => "UnarmedCombat";
    }
}
