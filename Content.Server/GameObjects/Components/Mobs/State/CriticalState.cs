using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.State
{
    public class CriticalState : SharedCriticalState
    {
        public override void EnterState(IEntity entity)
        {
            if (entity.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Critical);
            }

            if (entity.TryGetComponent(out ServerAlertsComponent status))
            {
                status.ShowAlert(AlertType.HumanCrit); //Todo: combine humancrit-0 and humancrit-1 into a gif and display it
            }

            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlay))
            {
                overlay.AddOverlay(SharedOverlayID.GradientCircleMaskOverlay);
            }

            if (entity.TryGetComponent(out StunnableComponent stun))
            {
                stun.CancelAll();
            }

            EntitySystem.Get<StandingStateSystem>().Down(entity);
        }

        public override void ExitState(IEntity entity)
        {
            if (entity.TryGetComponent(out ServerOverlayEffectsComponent overlay))
            {
                overlay.ClearOverlays();
            }
        }

        public override void UpdateState(IEntity entity) { }
    }
}
