using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;

namespace Content.Server.GameObjects.Components.Mobs.State
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

            if (entity.TryGetComponent(out ServerAlertsComponent? status))
            {
                status.ShowAlert(AlertType.HumanDead);
            }

            if (entity.TryGetComponent(out StunnableComponent? stun))
            {
                stun.CancelAll();
            }

            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, new AttemptDownEvent());
        }
    }
}
