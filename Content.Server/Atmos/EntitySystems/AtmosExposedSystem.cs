using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server.Atmos.EntitySystems
{
    /* doesn't seem to be a use for this at the moment, so it's disabled
    public sealed class AtmosExposedSystem : EntitySystem
    {}
    */

    /// <summary>
    /// Raises an event to an atmos exposed entity to have its various components respond to its current atmosphere.
    /// </summary>
    [ByRefEvent]
    public readonly struct AtmosExposedUpdateEvent
    {
        /// <summary>
        /// Coordinates of this entity.
        /// </summary>
        public readonly EntityCoordinates Coordinates;

        /// <summary>
        /// Current GasMixture the entity is in.
        /// </summary>
        public readonly GasMixture GasMixture;

        /// <summary>
        /// TransformComponent of the entity we're updating.
        /// </summary>
        public readonly TransformComponent Transform;

        /// <summary>
        /// Amount of time since the last AtmosExposedUpdateEvent.
        /// </summary>
        public readonly float DeltaTime;

        public AtmosExposedUpdateEvent(EntityCoordinates coordinates, GasMixture mixture, TransformComponent transform, float deltaTime)
        {
            Coordinates = coordinates;
            GasMixture = mixture;
            Transform = transform;
            DeltaTime = deltaTime;
        }
    }

    /// <summary>
    ///     Event that tries to query the mixture a certain entity is exposed to.
    ///     This is mainly intended for use with entities inside of containers.
    ///     This event is not raised for entities that are directly parented to the grid.
    /// </summary>
    [ByRefEvent]
    public struct AtmosExposedGetAirEvent
    {
        /// <summary>
        ///     The entity we want to query this for.
        /// </summary>
        public readonly Entity<TransformComponent> Entity;

        /// <summary>
        ///     The mixture that the entity is exposed to. Output parameter.
        /// </summary>
        public GasMixture? Gas = null;

        /// <summary>
        ///     Whether to excite the mixture, if possible.
        /// </summary>
        public readonly bool Excite = false;

        /// <summary>
        ///     Whether this event has been handled or not.
        ///     Check this before changing anything.
        /// </summary>
        public bool Handled = false;

        public AtmosExposedGetAirEvent(Entity<TransformComponent> entity, bool excite = false)
        {
            Entity = entity;
            Excite = excite;
        }
    }
}
