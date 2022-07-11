using Content.Server.AI.WorldState;
using Content.Server.AI.WorldState.States;

namespace Content.Server.AI.Utility.Considerations.Movement
{
    public sealed class TargetDistanceCon : Consideration
    {
        protected override float GetScore(Blackboard context)
        {
            var self = context.GetState<SelfState>().GetValue();
            var entities = IoCManager.Resolve<IEntityManager>();

            if (context.GetState<TargetEntityState>().GetValue() is not {Valid: true} target || entities.Deleted(target) ||
                entities.GetComponent<TransformComponent>(target).GridUid != entities.GetComponent<TransformComponent>(self).GridUid)
            {
                return 0.0f;
            }

            // Anything further than 100 tiles gets clamped
            return (entities.GetComponent<TransformComponent>(target).Coordinates.Position - entities.GetComponent<TransformComponent>(self).Coordinates.Position).Length / 100;
        }
    }
}
