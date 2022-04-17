using Content.Server.Maps.NameGenerators;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Station;

/// <summary>
/// A config for a station. Specifies name and job slots.
/// This is the only part of stations a downstream should ideally need to modify directly.
/// </summary>
[DataDefinition, PublicAPI]
public sealed class StationConfig
{
    /// <summary>
    /// The name template to use for the station.
    /// If there's a name generator this should follow it's required format.
    /// </summary>
    [DataField("mapNameTemplate", required: true)]
    public string StationNameTemplate { get; } = default!;

    /// <summary>
    /// Name generator to use for the station, if any.
    /// </summary>
    [DataField("nameGenerator")]
    public StationNameGenerator? NameGenerator { get; }

    /// <summary>
    /// Jobs used at round start should the station run out of job slots.
    /// Doesn't necessarily mean the station has infinite slots for the given jobs mid-round!
    /// </summary>
    [DataField("overflowJobs", required: true, customTypeSerializer:typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string> OverflowJobs { get; } = default!;

    /// <summary>
    /// Index of all jobs available on the station, of form
    ///   job name: [round-start, mid-round]
    /// </summary>
    [DataField("availableJobs", required: true, customTypeSerializer:typeof(PrototypeIdDictionarySerializer<List<int?>, JobPrototype>))]
    public Dictionary<string, List<int?>> AvailableJobs { get; } = default!;
}

