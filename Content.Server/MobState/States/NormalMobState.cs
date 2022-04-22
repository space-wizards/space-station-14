using System;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;
using Robust.Shared.GameObjects;

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

            if (stateComponent.TryGetEarliestIncapacitatedState(threshold, out _, out var earliestThreshold) && damageable.TotalDamage>0)
            {
                modifier = (short) (damageable.TotalDamage / (earliestThreshold / 6f)); //6 hurt states including the first one and crit state
                if (modifier < 1)
                { //this is here so if this comes up with a decimal between 0 and 1 we just use the first "hurt" state by default
                    modifier = 1;
                }
            }
            EntitySystem.Get<AlertsSystem>().ShowAlert(entity, AlertType.HumanHealth, modifier);
        }
    }
}
