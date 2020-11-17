using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Shared.GameObjects.Components.Damage;
using Content.Shared.GameObjects.Components.Mobs.State;

namespace Content.Server.AI.Utility.Considerations.Combat
{
    public sealed class TargetIsCritCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var target = context.GetState<TargetEntityState>().GetValue();

            if (target == null || !target.TryGetComponent(out IMobStateComponent mobState))
            {
                return 0.0f;
            }

            if (mobState.IsCritical())
            {
                return 1.0f;
            }

            return 0.0f;
        }
    }
}
