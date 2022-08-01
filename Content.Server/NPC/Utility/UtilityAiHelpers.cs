using Content.Server.NPC.Components;
using Content.Server.NPC.Utility.AiLogic;
using Content.Server.NPC.WorldState;

namespace Content.Server.NPC.Utility
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
