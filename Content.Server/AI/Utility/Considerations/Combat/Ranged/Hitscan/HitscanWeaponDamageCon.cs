using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.GameObjects.Components.Weapon.Ranged.Hitscan;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged.Hitscan
{
    public sealed class HitscanWeaponDamageCon : Consideration
    {
        public HitscanWeaponDamageCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var weapon = context.GetState<WeaponEntityState>().GetValue();

            if (weapon == null || !weapon.TryGetComponent(out HitscanWeaponComponent hitscanWeaponComponent))
            {
                return 0.0f;
            }

            // Just went with max health
            return hitscanWeaponComponent.Damage / 300.0f;
        }
    }
}
