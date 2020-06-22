using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Weapon.Melee;

namespace Content.Server.AI.Utility.Considerations.Combat.Melee
{
    public sealed class HasMeleeWeaponCon : Consideration
    {
        public HasMeleeWeaponCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            foreach (var item in context.GetState<InventoryState>().GetValue())
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
