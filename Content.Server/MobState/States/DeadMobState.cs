using System;
using Content.Server.Stunnable;
using Content.Server.Stunnable.Components;
using Content.Shared.Alert;
using Content.Shared.MobState;
using Content.Shared.MobState.State;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.MobState.States
{
    public class DeadMobState : SharedDeadMobState
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
