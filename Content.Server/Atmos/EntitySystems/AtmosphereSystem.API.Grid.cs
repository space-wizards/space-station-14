using JetBrains.Annotations;

namespace Content.Server.Atmos.EntitySystems;

public partial class AtmosphereSystem
{
    /// <summary>
    /// Checks if a grid has an atmosphere.
    /// </summary>
    /// <param name="gridUid">The grid to check.</param>
    /// <returns>True if the grid has an atmosphere, false otherwise.</returns>
    [PublicAPI]
    public bool HasAtmosphere(EntityUid gridUid)
    {
        return _atmosQuery.HasComponent(gridUid);
    }

    /// <summary>
    /// Sets whether a grid is simulated by Atmospherics.
    /// </summary>
    /// <param name="gridUid">The grid to set.</param>
    /// <param name="simulated">Whether the grid should be simulated.</param>
    /// <returns>>True if the grid's simulated state was changed, false otherwise.</returns>
    [PublicAPI]
    public bool SetSimulatedGrid(EntityUid gridUid, bool simulated)
    {
        // TODO ATMOS this event literally has no subscribers. Did this just get silently refactored out?
        var ev = new SetSimulatedGridMethodEvent(gridUid, simulated);
        RaiseLocalEvent(gridUid, ref ev);

        return ev.Handled;
    }

    /// <summary>
    /// Checks whether a grid is simulated by Atmospherics.
    /// </summary>
    /// <param name="gridUid">The grid to check.</param>
    /// <returns>>True if the grid is simulated, false otherwise.</returns>
    public bool IsSimulatedGrid(EntityUid gridUid)
    {
        var ev = new IsSimulatedGridMethodEvent(gridUid);
        RaiseLocalEvent(gridUid, ref ev);

        return ev.Simulated;
    }
}
