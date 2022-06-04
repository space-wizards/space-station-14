using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;

namespace Content.Server.MobState.States
{
    public sealed class NormalMobState : SharedNormalMobState
    {
        public override void UpdateState(EntityUid entity, FixedPoint2 threshold, IEntityManager entityManager)
        {
            base.UpdateState(entity, threshold, entityManager);

            if (!entityManager.TryGetComponent(entity, out DamageableComponent? damageable))
            {
                return;
            }

            if (!entityManager.TryGetComponent(entity, out MobStateComponent? stateComponent))
            {
                return;
            }

            short modifier = 0;

            if (stateComponent.TryGetEarliestIncapacitatedState(threshold, out _, out var earliestThreshold) && damageable.TotalDamage != 0)
            {
                modifier = (short)(damageable.TotalDamage / (earliestThreshold / 5) + 1);
            }
            EntitySystem.Get<AlertsSystem>().ShowAlert(entity, AlertType.HumanHealth, modifier);
        }
    }
}
