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

        public AtmosExposedUpdateEvent(EntityCoordinates coordinates, GasMixture mixture)
        {
            Coordinates = coordinates;
            GasMixture = mixture;
        }
    }

    [ByRefEvent]
    public struct AtmosExposedGetAirEvent
    {
        public GasMixture? Gas;
    }
}
