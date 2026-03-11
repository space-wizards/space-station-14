using Content.Shared.Atmos;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /*
     Partial class to store common internal datatypes that Atmospherics uses.
     In general datatypes should not be stored inside the AtmosphereSystem class itself
     and instead should be in their own file in Atmos. This is just here for legacy reasons
     and will probably be migrated out soon.
     */

    public enum GasCompareResult
    {
        NoExchange = -2,
        TemperatureExchange = -1,
    }

    /// <summary>
    /// Data on the airtightness of a <see cref="TileAtmosphere"/>.
    /// Cached on the <see cref="TileAtmosphere"/> and updated during
    /// <see cref="AtmosphereSystem.ProcessRevalidate"/> if it was invalidated.
    /// </summary>
    /// <param name="BlockedDirections">The current directions blocked on this tile.
    /// This is where air cannot flow to.</param>
    /// <param name="NoAirWhenBlocked">Whether the tile can have air when blocking directions.
    /// Common for entities like thin windows which only block one face but can still have air in the residing tile.</param>
    /// <param name="FixVacuum">If true, Atmospherics will generate air (yes, creating matter from nothing)
    /// using the adjacent tiles as a seed if the airtightness is removed and the tile has no air.
    /// This allows stuff like airlocks that void air when becoming airtight to keep opening/closing without
    /// draining a room by continuously voiding air.</param>
    public readonly record struct AirtightData(
        AtmosDirection BlockedDirections,
        bool NoAirWhenBlocked,
        bool FixVacuum);
}
