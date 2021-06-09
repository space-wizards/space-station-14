using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems.AI.Steering
{
    public interface IAiSteeringRequest
    {
        SteeringStatus Status { get; set; }
        MapCoordinates TargetMap { get; }
        EntityCoordinates TargetGrid { get; }
        /// <summary>
        ///     How close we have to get before we've arrived
        /// </summary>
        float ArrivalDistance { get; }

        /// <summary>
        ///     How close the pathfinder needs to get. Typically you want this set lower than ArrivalDistance
        /// </summary>
        float PathfindingProximity { get; }

        /// <summary>
        ///     If we need LOS on the entity first before interaction
        /// </summary>
        bool RequiresInRangeUnobstructed { get; }

        /// <summary>
        ///     To avoid spamming InRangeUnobstructed we'll apply a cd to it.
        /// </summary>
        public float TimeUntilInteractionCheck { get; set; }
    }
}
