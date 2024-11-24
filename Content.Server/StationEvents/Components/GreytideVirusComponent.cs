using Content.Server.StationEvents.Events;
using Content.Shared.Destructible.Thresholds;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Greytide Virus event specific configuration
/// </summary>
[RegisterComponent, Access(typeof(GreytideVirusRule))]
public sealed partial class GreytideVirusRuleComponent : Component
{
    /// <summary>
    ///     Range from which the severity is randomly picked from.
    ///     Severity corresponds to the number of substations affected.
    /// </summary>
    [DataField]
    public MinMax SeverityRange = new(1, 3);
}
