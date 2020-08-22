using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems.AI.Steering
{
    public interface IAiSteeringRequest
    {
        SteeringStatus Status { get; set; }
        MapCoordinates TargetMap { get; }
        GridCoordinates TargetGrid { get; }
        /// <summary>
        /// How close we have to get before we've arrived
        /// </summary>
        float ArrivalDistance { get; }
        
        /// <summary>
        /// How close the pathfinder needs to get. Typically you want this set lower than ArrivalDistance
        /// </summary>
        float PathfindingProximity { get; }
    }
}