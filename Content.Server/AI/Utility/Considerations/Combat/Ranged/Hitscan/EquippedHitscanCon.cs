using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged.Hitscan
{
    public sealed class EquippedHitscanCon : Consideration
    {
        public EquippedHitscanCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var equipped = context.GetState<EquippedEntityState>().GetValue();

            if (equipped == null || !equipped.HasComponent<HitscanWeaponComponent>())
            {
                return 0.0f;
            }

            return 1.0f;
        }
    }
}
