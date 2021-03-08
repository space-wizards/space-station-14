using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;

namespace Content.Server.AI.Utility.Considerations.ActionBlocker
{
    public sealed class CanMoveCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var self = context.GetState<SelfState>().GetValue();
            if (!ActionBlockerSystem.CanMove(self))
            {
                return 0.0f;
            }

            return 1.0f;
        }
    }
}
