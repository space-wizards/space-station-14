using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;

namespace Content.Server.AI.Utility.Considerations.Movement
{
    public sealed class DistanceCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var self = context.GetState<SelfState>().GetValue();
            var target = context.GetState<TargetEntityState>().GetValue();
            if (target == null || target.Transform.GridID != self.Transform.GridID)
            {
                return 0.0f;
            }
            
            // Kind of just pulled a max distance out of nowhere. Add 0.01 just in case it's reaally far and we have no choice so it'll still be considered at least.
            return (target.Transform.GridPosition.Position - self.Transform.GridPosition.Position).Length / 100 + 0.01f;
        }
    }
}
