using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Shared.Movement;

namespace Content.Server.AI.Utility.Considerations.ActionBlocker
{
    public sealed class CanMoveCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var self = context.GetState<SelfState>().GetValue();

            if (self == null || !self.CanMove())
            {
                return 0.0f;
            }

            return 1.0f;
        }
    }
}
