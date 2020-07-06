using Content.Server.AI.Utility.Curves;
using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Shared.GameObjects.EntitySystems;

namespace Content.Server.AI.Utility.Considerations.ActionBlocker
{
    public sealed class CanMoveCon : Consideration
    {
        public CanMoveCon(IResponseCurve curve) : base(curve) {}

        public override float GetScore(Blackboard context)
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
