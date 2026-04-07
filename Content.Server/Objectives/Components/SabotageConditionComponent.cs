using Content.Shared.Whitelist;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Marks an objective as a sabotage objective, and defines what must be sabotaged for the objective to be marked as complete.
/// Relies on <see cref="CodeConditionComponent"/>, as most of these objectives lock out when evac docks.
/// </summary>
[RegisterComponent]
public sealed partial class SabotageConditionComponent : Component
{
    /// <summary>
    /// Whitelist for the entity you must hack via beacon in order to complete this objective.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist;

    /// <summary>
    /// Blacklist for entities that will not count for this objective. Takes priority over the whitelist.s
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Are there more criteria to success than just "stick the beacon on"?
    /// </summary>
    [DataField]
    public bool RequireConfirmation = false;
}
