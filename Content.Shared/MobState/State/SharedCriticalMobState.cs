using Content.Shared.Alert;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState.State
{
    /// <summary>
    ///     A state in which an entity is disabled from acting due to sufficient damage (considered unconscious).
    /// </summary>
    public abstract class SharedCriticalMobState : BaseMobState
    {
        protected override DamageState DamageState => DamageState.Critical;

        public override void EnterState(EntityUid uid, IEntityManager entityManager)
        {
            base.EnterState(uid, entityManager);

            if (entityManager.TryGetComponent(uid, out SharedAlertsComponent? status))
            {
                status.ShowAlert(AlertType.HumanCrit); // TODO: combine humancrit-0 and humancrit-1 into a gif and display it
            }

            EntitySystem.Get<StandingStateSystem>().Down(uid);

            if (entityManager.TryGetComponent(uid, out SharedAppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Critical);
            }
        }

        public override void ExitState(EntityUid uid, IEntityManager entityManager)
        {
            base.ExitState(uid, entityManager);

            EntitySystem.Get<StandingStateSystem>().Stand(uid);
        }
    }
}
