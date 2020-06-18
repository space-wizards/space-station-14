using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States.Combat;
using Content.Server.GameObjects.Components.Weapon.Ranged.Projectile;

namespace Content.Server.AI.Utility.Considerations.Combat.Ranged.Ballistic
{
    public class BallisticAmmoCon : Consideration
    {
        public BallisticAmmoCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
        {
            var weapon = context.GetState<WeaponEntityState>().GetValue();

            if (weapon == null || !weapon.TryGetComponent(out BallisticMagazineWeaponComponent ballistic))
            {
                return 0.0f;
            }

            var contained = ballistic.MagazineSlot.ContainedEntity;

            if (contained == null)
            {
                return 0.0f;
            }

            var mag = contained.GetComponent<BallisticMagazineComponent>();

            if (mag.CountLoaded == 0)
            {
                // TODO: Do this better
                return ballistic.GetChambered(0) != null ? 1.0f : 0.0f;
            }

            return (float) mag.CountLoaded / mag.Capacity;
        }
    }
}
