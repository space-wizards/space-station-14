using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /*
     All events that Atmospherics uses.

     To be honest most of these should be deprecated.
     Maybe event relays will have a place in Atmospherics, but at the moment
     Atmospherics isn't designed very well to allow multiple simulation backends to take advantage
     of stuff like this.
     */

    [ByRefEvent]
    private record struct SetSimulatedGridMethodEvent(
        EntityUid Grid,
        bool Simulated,
        bool Handled = false);

    [ByRefEvent]
    private record struct IsSimulatedGridMethodEvent(
        EntityUid Grid,
        bool Simulated = false,
        bool Handled = false);

    [ByRefEvent]
    private record struct GetAllMixturesMethodEvent(
        EntityUid Grid,
        bool Excite = false,
        IEnumerable<GasMixture>? Mixtures = null,
        bool Handled = false);

    [ByRefEvent]
    private record struct ReactTileMethodEvent(
        EntityUid GridId,
        Vector2i Tile,
        ReactionResult Result = default,
        bool Handled = false);

    [ByRefEvent]
    private record struct HotspotExtinguishMethodEvent(
        EntityUid Grid,
        Vector2i Tile,
        bool Handled = false);

    [ByRefEvent]
    private record struct IsHotspotActiveMethodEvent(
        EntityUid Grid,
        Vector2i Tile,
        bool Result = false,
        bool Handled = false);
}
