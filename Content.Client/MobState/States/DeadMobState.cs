using Content.Shared.MobState;
using Content.Shared.MobState.State;
using Content.Shared.Standing;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.MobState.States
{
    public class DeadMobState : SharedDeadMobState
    {
        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);

            if (entity.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Dead);
            }
        }
    }
}
