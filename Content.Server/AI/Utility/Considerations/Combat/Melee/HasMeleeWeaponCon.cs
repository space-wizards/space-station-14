using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.Weapon.Melee.Components;

namespace Content.Server.AI.Utility.Considerations.Combat.Melee
{
    public sealed class HasMeleeWeaponCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            foreach (var item in context.GetState<EnumerableInventoryState>().GetValue())
            {
                if (item.HasComponent<MeleeWeaponComponent>())
                {
                    return 1.0f;
                }
            }

            return 0.0f;
        }
    }
}
