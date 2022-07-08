using Content.Server.EUI;
using Content.Shared.CrewManifest;

namespace Content.Server.CrewManifest;

public sealed class CrewManifestEui : BaseEui
{
    private readonly CrewManifestSystem _crewManifest;
    /// <summary>
    ///     Current station that this EUI is tracking. If the player can't open
    ///     a crew manifest from their current grid, just show them
    ///     an error or something
    /// </summary>
    private readonly EntityUid _station;

    /// <summary>
    ///     Stations this EUI instance is currently tracking.
    /// </summary>
    public readonly HashSet<EntityUid> Stations = new();

    public CrewManifestEui(CrewManifestSystem crewManifestSystem)
    {
        _crewManifest = crewManifestSystem;
    }

    public override CrewManifestEuiState GetNewState()
    {
        var stations = new List<(EntityUid, CrewManifestEntries?)>();

        foreach (var station in Stations)
        {
            stations.Add((station, _crewManifest.GetCrewManifest(station)));
        }

        return new(stations);
    }
}
