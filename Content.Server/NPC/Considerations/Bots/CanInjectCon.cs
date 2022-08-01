using Content.Server.NPC.Tracking;
using Content.Server.NPC.Utility.Considerations;
using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Server.Silicons.Bots;
using Content.Shared.Damage;
using Content.Shared.MobState.Components;

namespace Content.Server.NPC.Considerations.Bots
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
