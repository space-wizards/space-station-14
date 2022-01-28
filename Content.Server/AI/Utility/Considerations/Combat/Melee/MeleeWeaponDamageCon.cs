using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.Weapon.Melee.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Combat.Melee
{
    public sealed class MeleeWeaponDamageCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<WeaponEntityState>().GetValue();

            if (target == null || !IoCManager.Resolve<IEntityManager>().TryGetComponent(target, out MeleeWeaponComponent? meleeWeaponComponent))
            {
                return 0.0f;
            }

            // Just went with max health
            return (meleeWeaponComponent.Damage.Total / 300.0f).Float();
        }
    }
}
