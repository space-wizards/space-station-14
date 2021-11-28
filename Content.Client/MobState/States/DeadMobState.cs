using Content.Shared.MobState;
using Content.Shared.MobState.State;
using Robust.Shared.GameObjects;

namespace Content.Client.MobState.States
{
    public class DeadMobState : SharedDeadMobState
    {
        public override void EnterState(EntityUid uid, IEntityManager entityManager)
        {
            base.EnterState(uid, entityManager);

            if (entityManager.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Dead);
            }
        }
    }
}
