using Content.Server.AI.Components;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.AI.WorldState;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.Utility
{
    public static class UtilityAiHelpers
    {
        public static Blackboard? GetBlackboard(IEntity entity)
        {
            if (!entity.TryGetComponent(out AiControllerComponent? aiControllerComponent))
            {
                return null;
            }

            if (aiControllerComponent is UtilityAi utilityAi)
            {
                return utilityAi.Blackboard;
            }

            return null;
        }
    }
}
