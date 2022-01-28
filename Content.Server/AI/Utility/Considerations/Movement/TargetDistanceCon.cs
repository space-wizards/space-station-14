using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.Utility.Considerations.Movement
{
    public sealed class TargetDistanceCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var self = context.GetState<SelfState>().GetValue();
            var entities = IoCManager.Resolve<IEntityManager>();

            if (context.GetState<TargetEntityState>().GetValue() is not {Valid: true} target || entities.Deleted(target) ||
                entities.GetComponent<TransformComponent>(target).GridID != entities.GetComponent<TransformComponent>(self).GridID)
            {
                return 0.0f;
            }

            // Anything further than 100 tiles gets clamped
            return (entities.GetComponent<TransformComponent>(target).Coordinates.Position - entities.GetComponent<TransformComponent>(self).Coordinates.Position).Length / 100;
        }
    }
}
