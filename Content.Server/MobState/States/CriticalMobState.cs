using Content.Server.Stunnable;
using Content.Server.Stunnable.Components;
using Content.Shared.MobState.State;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;

namespace Content.Server.MobState.States
{
    public class CriticalMobState : SharedCriticalMobState
    {
        public override void EnterState(IEntity entity)
        {
            base.EnterState(entity);

            if (entity.TryGetComponent(out StatusEffectsComponent? stun))
            {
                EntitySystem.Get<StatusEffectsSystem>().TryRemoveStatusEffect(entity.Uid, "Stun");
            }
        }
    }
}
