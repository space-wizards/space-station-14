using Content.Server.GameTicking.Rules;

namespace Content.Server.Station.Components;

/// <summary>
///     Marker component for stations where procedural variation using <see cref="RoundstartStationVariationRuleSystem"/>
///     has already run, so as to avoid running it again if another station is added.
/// </summary>
[RegisterComponent]
public sealed partial class StationVariationHasRunComponent : Component
{
}
