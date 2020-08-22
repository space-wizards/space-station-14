using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;

namespace Content.Server.AI.Utility.Considerations.Movement
{
    public sealed class TargetDistanceCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var self = context.GetState<SelfState>().GetValue();
            var target = context.GetState<TargetEntityState>().GetValue();
            if (target == null || target.Deleted || target.Transform.GridID != self.Transform.GridID)
            {
                return 0.0f;
            }
            
            // Anything further than 100 tiles gets clamped
            return (target.Transform.GridPosition.Position - self.Transform.GridPosition.Position).Length / 100;
        }
    }
}
