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
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Name of the map to use in generic messages, like the map vote.
    /// </summary>
    [DataField("mapName", required: true)]
    public string MapName { get; private set; } = default!;

    /// <summary>
    /// Relative directory path to the given map, i.e. `/Maps/saltern.yml`
    /// </summary>
    [DataField("mapPath", required: true)]
    public ResPath MapPath { get; private set; } = default!;

    [DataField("stations", required: true)]
    private Dictionary<string, StationConfig> _stations = new();

    /// <summary>
    /// The stations this map contains. The names should match with the BecomesStation components.
    /// </summary>
    public IReadOnlyDictionary<string, StationConfig> Stations => _stations;

    /// <summary>
    /// Performs a shallow clone of this map prototype, replacing <c>MapPath</c> with the argument.
    /// </summary>
    public GameMapPrototype Persistence(ResPath mapPath)
    {
        return new()
        {
            ID = ID,
            MapName = MapName,
            MapPath = mapPath,
            _stations = _stations
        };
    }
}
