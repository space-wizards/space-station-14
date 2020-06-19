using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Weapon.Ranged;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged
{
    public sealed class RangedWeaponEquippedCon : Consideration
    {
        public RangedWeaponEquippedCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var equipped = context.GetState<EquippedEntityState>().GetValue();

            if (equipped == null || !equipped.HasComponent<RangedWeaponComponent>())
            {
                return 0.0f;
            }

            return 1.0f;
        }
    }
}
