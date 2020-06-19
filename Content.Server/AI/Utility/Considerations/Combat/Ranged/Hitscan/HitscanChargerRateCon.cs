using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects.Components.Power.Chargers;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged.Hitscan
{
    public sealed class HitscanChargerRateCon : Consideration
    {
        public HitscanChargerRateCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();
            if (target == null || !target.TryGetComponent(out WeaponCapacitorChargerComponent weaponCharger))
            {
                return 0.0f;
            }

            // AI don't care about efficiency, psfft!
            return weaponCharger.TransferRatio;
        }
    }
}
