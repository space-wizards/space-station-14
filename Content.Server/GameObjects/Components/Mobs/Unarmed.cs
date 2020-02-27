using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Mobs
{

    [RegisterComponent]
    public class UnarmedComponent : MeleeWeaponComponent
    {
        public override string Name => "Unarmed";

        /// <summary>
        /// Called when a human attempts to attack with nothing in their hand
        /// </summary>
        public void DoUnarmedAttack(AttackEventArgs eventArgs)
        {
            DoAttack(eventArgs);
        }
    }
}
