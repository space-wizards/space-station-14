using System.Collections.Generic;
using Content.Server.Maps.NameGenerators;
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
public class GameMapPrototype : IPrototype
{
    /// <inheritdoc/>
    [DataField("id", required: true)]
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
    /// Name of the given map.
    /// </summary>
    [DataField("mapNameTemplate")]
    public string? MapNameTemplate { get; } = default!;

    /// <summary>
    /// Name generator to use for the map, if any.
    /// </summary>
    [DataField("nameGenerator")]
    public GameMapNameGenerator? NameGenerator { get; } = default!;

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

    /// <summary>
    /// Jobs used at round start should the station run out of job slots.
    /// Doesn't necessarily mean the station has infinite slots for the given jobs midround!
    /// </summary>
    [DataField("overflowJobs", required: true, customTypeSerializer:typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string> OverflowJobs { get; } = default!;

    /// <summary>
    /// Index of all jobs available on the station, of form
    ///  jobname: [roundstart, midround]
    /// </summary>
    [DataField("availableJobs", required: true, customTypeSerializer:typeof(PrototypeIdDictionarySerializer<List<int>, JobPrototype>))]
    public Dictionary<string, List<int>> AvailableJobs { get; } = default!;
}
