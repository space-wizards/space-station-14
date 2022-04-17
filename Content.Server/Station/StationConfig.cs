using Content.Server.Maps.NameGenerators;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Station;

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
    public StationNameGenerator? NameGenerator { get; } = default!;

    /// <summary>
    /// Jobs used at round start should the station run out of job slots.
    /// Doesn't necessarily mean the station has infinite slots for the given jobs midround!
    /// </summary>
    [DataField("overflowJobs", required: true, customTypeSerializer:typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string> OverflowJobs { get; } = default!;

    /// <summary>
    /// Index of all jobs available on the station, of form
    ///   jobname: [roundstart, midround]
    /// </summary>
    [DataField("availableJobs", required: true, customTypeSerializer:typeof(PrototypeIdDictionarySerializer<List<int?>, JobPrototype>))]
    public Dictionary<string, List<int?>> AvailableJobs { get; } = default!;
}

