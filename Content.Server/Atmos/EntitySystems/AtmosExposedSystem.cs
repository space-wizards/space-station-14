using Robust.Shared.Map;

namespace Content.Server.Atmos.EntitySystems
{
    /* doesn't seem to be a use for this at the moment, so it's disabled
    public sealed class AtmosExposedSystem : EntitySystem
    {}
    */

    [ByRefEvent]
    public readonly struct AtmosExposedUpdateEvent
    {
        public readonly EntityCoordinates Coordinates;
        public readonly GasMixture GasMixture;
        public readonly TransformComponent Transform;

        public AtmosExposedUpdateEvent(EntityCoordinates coordinates, GasMixture mixture, TransformComponent transform)
        {
            Coordinates = coordinates;
            GasMixture = mixture;
            Transform = transform;
        }
    }

    /// <summary>
    ///     Event that tries to query the mixture a certain entity is exposed to.
    /// </summary>
    [ByRefEvent]
    public struct AtmosExposedGetAirEvent
    {
        /// <summary>
        ///     The entity we want to query this for.
        /// </summary>
        public readonly EntityUid Entity;

        /// <summary>
        ///     The mixture that the entity is exposed to. Output parameter.
        /// </summary>
        public GasMixture? Gas = null;

        /// <summary>
        ///     Whether to invalidate the mixture, if possible.
        /// </summary>
        public bool Invalidate = false;

        /// <summary>
        ///     Whether this event has been handled or not.
        ///     Check this before changing anything.
        /// </summary>
        public bool Handled = false;

        public AtmosExposedGetAirEvent(EntityUid entity, bool invalidate = false)
        {
            Entity = entity;
            Invalidate = invalidate;
        }
    }
}
