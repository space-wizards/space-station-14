using Content.Server.AI.Components;
using Content.Server.AI.Utility.AiLogic;
using Content.Server.AI.WorldState;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility
{
    public static class UtilityAiHelpers
    {
        public static Blackboard? GetBlackboard(EntityUid entity)
        {
            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(entity, out AiControllerComponent? aiControllerComponent))
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
