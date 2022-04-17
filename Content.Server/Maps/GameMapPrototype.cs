using System.Collections.Generic;
using Content.Server.Maps.NameGenerators;
using Content.Server.Station;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Server.Maps;

/// <summary>
/// Prototype data for a game map.
/// </summary>
[Prototype("gameMap")]
public sealed class GameMapPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Minimum players for the given map.
    /// </summary>
    [DataField("minPlayers", required: true)]
    public uint MinPlayers { get; }

    /// <summary>
    /// Maximum players for the given map.
    /// </summary>
    [DataField("maxPlayers")]
    public uint MaxPlayers { get; } = uint.MaxValue;

    /// <summary>
    /// Name of the map to use in generic messages, like the map vote.
    /// </summary>
    [DataField("mapName", required: true)]
    public string MapName { get; } = default!;

    /// <summary>
    /// Relative directory path to the given map, i.e. `Maps/saltern.yml`
    /// </summary>
    [DataField("mapPath", required: true)]
    public ResourcePath MapPath { get; } = default!;

    /// <summary>
    /// Controls if the map can be used as a fallback if no maps are eligible.
    /// </summary>
    [DataField("fallback")]
    public bool Fallback { get; }

    /// <summary>
    /// Controls if the map can be voted for.
    /// </summary>
    [DataField("votable")]
    public bool Votable { get; } = true;

    [DataField("conditions")]
    public List<GameMapCondition> Conditions { get; } = new();

    [DataField("stations")]
    private Dictionary<string, StationConfig> _stations = new();

    /// <summary>
    /// The stations this map contains. The names should match with the BecomesStation components.
    /// </summary>
    public IReadOnlyDictionary<string, StationConfig> Stations => _stations;
}
