using Content.Server.Maps.NameGenerators;
using JetBrains.Annotations;

namespace Content.Server.Station;

/// <summary>
/// A config for a station. Specifies name and job slots.
/// This is the only part of stations a downstream should ideally need to modify directly.
/// </summary>
/// <remarks>
/// Forks should not directly edit existing parts of this class.
/// Make a new partial for your fancy new feature, it'll save you time later.
/// </remarks>
[DataDefinition, PublicAPI]
public sealed partial class StationConfig
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
}

