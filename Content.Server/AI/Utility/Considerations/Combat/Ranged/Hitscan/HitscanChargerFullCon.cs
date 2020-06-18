using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects.Components.Power.Chargers;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged.Hitscan
{
    public sealed class HitscanChargerFullCon : Consideration
    {
        public HitscanChargerFullCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null ||
                !target.TryGetComponent(out WeaponCapacitorChargerComponent chargerComponent) ||
                chargerComponent.HeldItem != null)
            {
                return 1.0f;
            }

            return 0.0f;
        }
    }
}
