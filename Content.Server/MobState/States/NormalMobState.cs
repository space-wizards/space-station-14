using System;
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

            if (stateComponent.TryGetEarliestIncapacitatedState(threshold, out _, out var earliestThreshold))
            {
                modifier = (short) (damageable.TotalDamage / (earliestThreshold / 7f));
            }

            EntitySystem.Get<AlertsSystem>().ShowAlert(entity, AlertType.HumanHealth, modifier);
        }
    }
}
