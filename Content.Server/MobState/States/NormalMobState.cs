using Content.Server.Alert;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;
using Robust.Shared.GameObjects;

namespace Content.Server.MobState.States
{
    public class NormalMobState : SharedNormalMobState
    {
        public override void UpdateState(IEntity entity, FixedPoint2 threshold)
        {
            base.UpdateState(entity, threshold);

            if (!entity.TryGetComponent(out DamageableComponent? damageable))
            {
                return;
            }

            if (!entity.TryGetComponent(out ServerAlertsComponent? alerts))
            {
                return;
            }

            if (!entity.TryGetComponent(out MobStateComponent? stateComponent))
            {
                return;
            }

            short modifier = 0;

            if (stateComponent.TryGetEarliestIncapacitatedState(threshold, out _, out var earliestThreshold))
            {
                modifier = (short) (damageable.TotalDamage / (earliestThreshold / 7f));
            }

            alerts.ShowAlert(AlertType.HumanHealth, modifier);
        }
    }
}
