using Content.Server.Maps.NameGenerators;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Station;

/// <summary>
/// A config for a station. Specifies name and component modifications.
/// </summary>
[DataDefinition, PublicAPI]
public sealed partial class StationConfig
{
    [DataField("stationProto", required: true)]
    public string StationPrototype = default!;

    [DataField("components", required: true)]
    public ComponentRegistry StationComponentOverrides = default!;
}

