using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that all other living players are eliminated.
/// The owner is excluded from the target set.
/// </summary>
[RegisterComponent, Access(typeof(KillAllOthersConditionSystem))]
public sealed partial class KillAllOthersConditionComponent : Component
{
    /// <summary>
    /// Whether each target must be dead. Defaults to true.
    /// </summary>
    [DataField]
    public bool RequireDead = true;
}
