using Content.Server.Standing;
using Content.Server.Stunnable.Components;
using Content.Shared.MobState;
using Content.Shared.MobState.State;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.MobState.States
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

            EntitySystem.Get<StandingStateSystem>().Down(entity);
        }
    }
}
