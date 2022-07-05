using Content.Shared.Alert;
using Content.Shared.MobState.State;
using Content.Shared.StatusEffect;

namespace Content.Server.MobState.States
{
    public sealed class DeadMobState : SharedDeadMobState
    {
        public override void EnterState(EntityUid uid, IEntityManager entityManager)
        {
            base.EnterState(uid, entityManager);

            EntitySystem.Get<AlertsSystem>().ShowAlert(uid, AlertType.HumanDead);

            if (entityManager.TryGetComponent(uid, out StatusEffectsComponent? stun))
            {
                EntitySystem.Get<StatusEffectsSystem>().TryRemoveStatusEffect(uid, "Stun");
            }
        }
    }
}
