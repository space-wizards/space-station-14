using Content.Server.Maps.NameGenerators;

namespace Content.Server.Station.Components;

/// <summary>
/// This is used for setting up a station's name.
/// </summary>
[RegisterComponent]
public sealed partial class StationNameSetupComponent : Component
{
    /// <summary>
    /// The name template to use for the station.
    /// If there's a name generator this should follow it's required format.
    /// </summary>
    [DataField("mapNameTemplate", required: true)]
    public string StationNameTemplate { get; private set; } = default!;

    /// <summary>
    /// Name generator to use for the station, if any.
    /// </summary>
    [DataField("nameGenerator")]
    public StationNameGenerator? NameGenerator { get; private set; }
}
