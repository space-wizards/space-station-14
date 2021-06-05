using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Mobs.State
{
    public class CriticalMobState : SharedCriticalMobState
    {
        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);

            if (entity.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(DamageStateVisuals.State, DamageState.Critical);
            }

            if (entity.TryGetComponent(out StunnableComponent? stun))
            {
                stun.CancelAll();
            }

            entity.EntityManager.EventBus.RaiseLocalEvent(entity.Uid, new AttemptDownEvent());
        }
    }
}
