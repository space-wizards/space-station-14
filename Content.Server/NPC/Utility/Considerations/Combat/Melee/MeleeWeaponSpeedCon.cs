using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States.Combat;
using Content.Server.Weapon.Melee.Components;

namespace Content.Server.NPC.Utility.Considerations.Combat.Melee
{
    public sealed class MeleeWeaponSpeedCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<WeaponEntityState>().GetValue();

            if (target == null || !IoCManager.Resolve<IEntityManager>().TryGetComponent(target, out MeleeWeaponComponent? meleeWeaponComponent))
            {
                return 0.0f;
            }

            return meleeWeaponComponent.ArcCooldownTime / 10.0f;
        }
    }
}
