using Content.Server.Atmos.Components;
using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Atmos.EntitySystems
{
    /* doesn't seem to be a use for this at the moment, so it's disabled
    public class AtmosExposedSystem : EntitySystem
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
}
