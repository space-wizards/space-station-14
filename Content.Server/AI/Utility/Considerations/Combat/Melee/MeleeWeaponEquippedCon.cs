using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.Weapon.Melee.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Combat.Melee
{
    public sealed class MeleeWeaponEquippedCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var equipped = context.GetState<EquippedEntityState>().GetValue();

            if (equipped == null)
            {
                return 0.0f;
            }

            return IoCManager.Resolve<IEntityManager>().HasComponent<MeleeWeaponComponent>(equipped) ? 1.0f : 0.0f;
        }
    }
}
