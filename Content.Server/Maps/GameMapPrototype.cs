using Content.Server.Station;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Diagnostics;

namespace Content.Server.Maps;

/// <summary>
/// Prototype data for a game map.
/// </summary>
/// <remarks>
/// Forks should not directly edit existing parts of this class.
/// Make a new partial for your fancy new feature, it'll save you time later.
/// </remarks>
[Prototype("gameMap"), PublicAPI]
[DebuggerDisplay("GameMapPrototype [{ID} - {MapName}]")]
public sealed partial class GameMapPrototype : IPrototype
{
    public GameMapPrototype() { }
    public GameMapPrototype(string mapName, ResourcePath mapPath, Dictionary<string, StationConfig> stations)
    {
        MapPath = mapPath;
        _stations = stations;
        ID = mapName;
        MapName = mapName;
    }

    public static GameMapPrototype Persistence(ResourcePath mapPath, string mapName = "Empty", string defaultJob = "Passenger")
    {
        var stations = new Dictionary<string, StationConfig>();
        var defaultAvailableJobs = new Dictionary<string, List<int?>>()
        {
            { defaultJob, new List<int?>() { { -1 }, { -1 } } }
        };
        var defaultOverflowJobs = new List<string>()
        {
            { defaultJob },
        };
        var defaultStation = new StationConfig(mapName, defaultAvailableJobs, defaultOverflowJobs);
        stations.Add(mapName, defaultStation);
        return new GameMapPrototype(mapName, mapPath, stations);
    }

    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Name of the map to use in generic messages, like the map vote.
    /// </summary>
    [DataField("mapName", required: true)]
    public string MapName { get; } = default!;

    /// <summary>
    /// Relative directory path to the given map, i.e. `/Maps/saltern.yml`
    /// </summary>
    [DataField("mapPath", required: true)]
    public ResourcePath MapPath { get; } = default!;

    [DataField("stations", required: true)]
    private Dictionary<string, StationConfig> _stations = new();

    /// <summary>
    /// The stations this map contains. The names should match with the BecomesStation components.
    /// </summary>
    public IReadOnlyDictionary<string, StationConfig> Stations => _stations;
}
