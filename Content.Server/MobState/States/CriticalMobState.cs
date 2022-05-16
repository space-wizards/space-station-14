using Content.Shared.MobState.State;
using Content.Shared.StatusEffect;

namespace Content.Server.MobState.States
{
    public sealed class CriticalMobState : SharedCriticalMobState
    {
        public override void EnterState(EntityUid uid, IEntityManager entityManager)
        {
            base.EnterState(uid, entityManager);

            if (entityManager.TryGetComponent(uid, out StatusEffectsComponent? stun))
            {
                EntitySystem.Get<StatusEffectsSystem>().TryRemoveStatusEffect(uid, "Stun");
            }
        }
    }
}
