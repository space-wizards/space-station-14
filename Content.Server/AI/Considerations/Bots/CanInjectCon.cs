using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.AI.Tracking;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;
using Content.Server.Silicons.Bots;

namespace Content.Server.AI.Utility.Considerations.Bot
{
    public sealed class CanInjectCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || !entMan.TryGetComponent(target, out DamageableComponent? damageableComponent))
                return 0;

            if (entMan.TryGetComponent(target, out RecentlyInjectedComponent? recently))
                return 0f;

            if (!entMan.TryGetComponent(target, out MobStateComponent? mobState) || mobState.IsDead())
                return 0f;

            if (damageableComponent.TotalDamage == 0)
                return 0f;

            if (damageableComponent.TotalDamage <= MedibotComponent.StandardMedDamageThreshold)
                return 1f;

            if (damageableComponent.TotalDamage >= MedibotComponent.EmergencyMedDamageThreshold)
                return 1f;

            return 0f;
        }
    }
}
