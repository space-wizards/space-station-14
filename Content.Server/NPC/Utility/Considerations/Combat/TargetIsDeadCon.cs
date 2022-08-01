using Content.Server.NPC.WorldState;
using Content.Server.NPC.WorldState.States;
using Content.Shared.MobState.Components;

namespace Content.Server.NPC.Utility.Considerations.Combat
{
    public sealed class TargetIsDeadCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || !IoCManager.Resolve<IEntityManager>().TryGetComponent(target, out MobStateComponent? mobState))
            {
                return 0.0f;
            }

            if (mobState.IsDead())
            {
                return 1.0f;
            }

            return 0.0f;
        }
    }
}
