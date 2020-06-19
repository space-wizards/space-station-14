using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged.Hitscan
{
    public sealed class HitscanWeaponEquippedCon : Consideration
    {
        public HitscanWeaponEquippedCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var equipped = context.GetState<EquippedEntityState>().GetValue();

            if (equipped == null)
            {
                return 0.0f;
            }

            return equipped.HasComponent<HitscanWeaponComponent>() ? 1.0f : 0.0f;
        }
    }
}
