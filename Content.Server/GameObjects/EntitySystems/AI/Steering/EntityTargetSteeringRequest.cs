using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems.AI.Steering
{
    public sealed class EntityTargetSteeringRequest : IAiSteeringRequest
    {
        public SteeringStatus Status { get; set; } = SteeringStatus.Pending;
        public MapCoordinates TargetMap => _target.Transform.MapPosition;
        public GridCoordinates TargetGrid => _target.Transform.GridPosition;
        public IEntity Target => _target;
        private IEntity _target;
        /// <inheritdoc />
        public float ArrivalDistance { get; }
        /// <inheritdoc />
        public float PathfindingProximity { get; }
        /// <summary>
        /// How far the target can move before we re-path
        /// </summary>
        public float TargetMaxMove { get; } = 1.5f;

        /// <summary>
        /// If we need LOS on the entity first before interaction
        /// </summary>
        public bool RequiresInRangeUnobstructed { get; }
        
        public EntityTargetSteeringRequest(IEntity target, float arrivalDistance, float pathfindingProximity = 0.5f, bool requiresInRangeUnobstructed = false)
        {
            _target = target;
            ArrivalDistance = arrivalDistance;
            PathfindingProximity = pathfindingProximity;
            RequiresInRangeUnobstructed = requiresInRangeUnobstructed;
        }
    }
}