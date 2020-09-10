using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems.AI.Steering
{
    public sealed class GridTargetSteeringRequest : IAiSteeringRequest
    {
        public SteeringStatus Status { get; set; } = SteeringStatus.Pending;
        public MapCoordinates TargetMap { get; }
        public EntityCoordinates TargetGrid { get; }
        /// <inheritdoc />
        public float ArrivalDistance { get; }
        /// <inheritdoc />
        public float PathfindingProximity { get; }

        public bool RequiresInRangeUnobstructed { get; }

        public float TimeUntilInteractionCheck { get; set; } = 0.0f;


        public GridTargetSteeringRequest(EntityCoordinates targetGrid, float arrivalDistance, float pathfindingProximity = 0.5f, bool requiresInRangeUnobstructed = false)
        {
            // Get it once up front so we the manager doesn't have to continuously get it
            var entityManager = IoCManager.Resolve<IEntityManager>();
            TargetMap = targetGrid.ToMap(entityManager);
            TargetGrid = targetGrid;
            ArrivalDistance = arrivalDistance;
            PathfindingProximity = pathfindingProximity;
            RequiresInRangeUnobstructed = requiresInRangeUnobstructed;
        }
    }
}
