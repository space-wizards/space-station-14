using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged.Hitscan
{
    public sealed class HitscanChargeCon : Consideration
    {
        public HitscanChargeCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var weapon = context.GetState<WeaponEntityState>().GetValue();

            if (weapon == null || !weapon.TryGetComponent(out HitscanWeaponComponent hitscanWeaponComponent))
            {
                return 0.0f;
            }

            return hitscanWeaponComponent.CapacitorComponent.Charge / hitscanWeaponComponent.CapacitorComponent.Capacity;
        }
    }
}
