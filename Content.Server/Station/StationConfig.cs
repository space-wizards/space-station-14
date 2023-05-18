using Content.Server.Maps.NameGenerators;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Station;

/// <summary>
/// A config for a station. Specifies name and component modifications.
/// </summary>
[DataDefinition, PublicAPI]
public sealed class StationConfig
{
    [DataField("stationProto", required: true)]
    public string StationPrototype = default!;

    [DataField("components", required: true)]
    public ComponentRegistry StationComponentOverrides = default!;

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
}

