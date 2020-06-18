using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Weapon.Ranged.Projectile;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged.Ballistic
{
    public class EquippedBallisticCon : Consideration
    {
        public EquippedBallisticCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var equipped = context.GetState<EquippedEntityState>().GetValue();

            if (equipped == null || !equipped.HasComponent<BallisticMagazineWeaponComponent>())
            {
                return 0.0f;
            }

            return 1.0f;
        }
    }
}
