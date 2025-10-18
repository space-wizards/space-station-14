using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that a player doesn't kill more than a set number of characters, or it fails.
/// </summary>
[RegisterComponent, Access(typeof(KillLimitConditionSystem))]
public sealed partial class KillLimitConditionComponent : Component
{
    /// <summary>
    /// The number of kills that are permissible for this condition; set upon the objective being assigned.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> KillList = new();

    /// <summary>
    /// The number of kills that are permissible for this condition; set upon the objective being assigned.
    /// </summary>
    [DataField]
    public int PermissibleKillCount;

    /// <summary>
    /// The minimum roll for permissible kills for this objective.
    /// </summary>
    [DataField]
    public int MinKillCount = 5;

    /// <summary>
    /// The maximum roll for permissible kills for this objective.
    /// </summary>
    [DataField]
    public int MaxKillCount = 5;

    /// <summary>
    /// If true, an entity that gets revived will be removed from the kill limit tracker.
    /// </summary>
    [DataField]
    public bool AllowReviving = true;

    /// <summary>
    /// The title of the objective. Takes the kill limit as input as "limit".
    /// </summary>
    [DataField]
    public LocId ObjectiveTitle = "objective-condition-kill-limit-title";

    /// <summary>
    /// The title of the objective. Takes the kill limit as input as "limit", and kill count as "value".
    /// </summary>
    [DataField]
    public LocId ObjectiveDescription = "objective-condition-kill-limit-description";
}
