using Content.Server.StationEvents.Events;
using Content.Shared.Access;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

/// <summary>
///     Greytide Virus event specific configuration
/// </summary>
[RegisterComponent, Access(typeof(GreytideVirusRule))]
public sealed partial class GreytideVirusRuleComponent : Component
{
    /// <summary>
    ///     Range from which the severity is randomly picked from.
    /// </summary>
    [DataField]
    public MinMax SeverityRange = new(1, 3);

    /// <summary>
    ///     Severity corresponding to the number of access groups affected.
    ///     Will pick randomly from the SeverityRange if not specified.
    /// </summary>
    [DataField]
    public int? Severity;

    /// <summary>
    ///     Access groups to pick from.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessGroupPrototype>> AccessGroups = new();

    /// <summary>
    ///     Entities with this access level will be ignored.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> Blacklist = new();
}
