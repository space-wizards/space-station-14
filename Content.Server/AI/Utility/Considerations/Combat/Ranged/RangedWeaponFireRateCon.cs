using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.GameObjects.Components.Weapon.Ranged;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged
{
    public class RangedWeaponFireRateCon : Consideration
    {
        public RangedWeaponFireRateCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var weapon = context.GetState<WeaponEntityState>().GetValue();

            if (weapon == null || !weapon.TryGetComponent(out RangedWeaponComponent ranged))
            {
                return 0.0f;
            }

            return ranged.FireRate / 100.0f;
        }
    }
}
