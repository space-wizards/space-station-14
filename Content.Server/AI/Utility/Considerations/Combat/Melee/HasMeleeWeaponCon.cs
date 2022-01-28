using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.Weapon.Melee.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Combat.Melee
{
    public sealed class HasMeleeWeaponCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            foreach (var item in context.GetState<EnumerableInventoryState>().GetValue())
            {
                if (IoCManager.Resolve<IEntityManager>().HasComponent<MeleeWeaponComponent>(item))
                {
                    return 1.0f;
                }
            }

            return 0.0f;
        }
    }
}
