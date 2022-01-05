using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.AI.Steering
{
    public sealed class EntityTargetSteeringRequest : IAiSteeringRequest
    {
        public SteeringStatus Status { get; set; } = SteeringStatus.Pending;
        public MapCoordinates TargetMap => IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(_target).MapPosition;
        public EntityCoordinates TargetGrid => IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(_target).Coordinates;
        public EntityUid Target => _target;
        private readonly EntityUid _target;

        /// <inheritdoc />
        public float ArrivalDistance { get; }

        /// <inheritdoc />
        public float PathfindingProximity { get; }

        /// <summary>
        ///     How far the target can move before we re-path
        /// </summary>
        public float TargetMaxMove { get; } = 1.5f;

        /// <inheritdoc />
        public bool RequiresInRangeUnobstructed { get; }

        /// <summary>
        ///     To avoid spamming InRangeUnobstructed we'll apply a cd to it.
        /// </summary>
        public float TimeUntilInteractionCheck { get; set; }

        public EntityTargetSteeringRequest(EntityUid target, float arrivalDistance, float pathfindingProximity = 0.5f, bool requiresInRangeUnobstructed = false)
        {
            _target = target;
            ArrivalDistance = arrivalDistance;
            PathfindingProximity = pathfindingProximity;
            RequiresInRangeUnobstructed = requiresInRangeUnobstructed;
        }
    }
}
