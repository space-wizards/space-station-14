using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Inventory;
using Content.Server.GameObjects.Components.Weapon.Melee;

namespace Content.Server.AI.Utility.Considerations.Combat.Melee
{
    public sealed class MeleeWeaponEquippedCon : Consideration
    {
        public MeleeWeaponEquippedCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var equipped = context.GetState<EquippedEntityState>().GetValue();

            if (equipped == null)
            {
                return 0.0f;
            }

            return equipped.HasComponent<MeleeWeaponComponent>() ? 1.0f : 0.0f;
        }
    }
}
