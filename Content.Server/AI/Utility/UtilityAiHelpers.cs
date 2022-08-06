using Content.Server.AI.Components;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.AI.WorldState;

namespace Content.Server.AI.Utility
{
    public static class UtilityAiHelpers
    {
        public static Blackboard? GetBlackboard(EntityUid entity)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out NPCComponent? aiControllerComponent))
            {
                return null;
            }

            if (aiControllerComponent is UtilityNPCComponent utilityAi)
            {
                return utilityAi.Blackboard;
            }

            return null;
        }
    }
}
