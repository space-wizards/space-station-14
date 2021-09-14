using Content.Server.Stunnable.Components;
using Content.Shared.MobState.State;
using Robust.Shared.GameObjects;

namespace Content.Server.MobState.States
{
    public class CriticalMobState : SharedCriticalMobState
    {
        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);

            if (entity.TryGetComponent(out StunnableComponent? stun))
            {
                stun.CancelAll();
            }
        }
    }
}
