using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Weapon.Melee;
using Content.Server.GameObjects.Components.Weapon.Ranged;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged
{
    public sealed class HeldRangedWeaponsCon : Consideration
    {
        public HeldRangedWeaponsCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var count = 0;
            const int max = 3;

            foreach (var item in context.GetState<InventoryState>().GetValue())
            {
                if (item.HasComponent<RangedWeaponComponent>())
                {
                    count++;
                }
            }

            return (float) count / max;
        }
    }
}
