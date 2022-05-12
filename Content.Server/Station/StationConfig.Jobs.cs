using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Station;

public sealed partial class StationConfig
{
    [DataField("overflowJobs", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    private readonly List<string> _overflowJobs = default!;

    /// <summary>
    /// Jobs used at round start should the station run out of job slots.
    /// Doesn't necessarily mean the station has infinite slots for the given jobs mid-round!
    /// </summary>
    public IReadOnlyList<string> OverflowJobs => _overflowJobs;


    [DataField("availableJobs", required: true,
        customTypeSerializer: typeof(PrototypeIdDictionarySerializer<List<int?>, JobPrototype>))]
    private readonly Dictionary<string, List<int?>> _availableJobs = default!;

    /// <summary>
    /// Index of all jobs available on the station, of form
    ///   job name: [round-start, mid-round]
    /// </summary>
    public IReadOnlyDictionary<string, List<int?>> AvailableJobs => _availableJobs;
}
