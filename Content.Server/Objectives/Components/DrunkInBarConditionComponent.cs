using Content.Shared.Objectives.Components;

namespace Content.Server.Objectives.Components;

/// <summary>
/// A condition that requires a player to get the drunk effect near a beacon with the <see cref="DrunkInBarTargetComponent"/> component.
/// </summary>
[RegisterComponent]
public sealed partial class DrunkInBarConditionComponent : Component
{
    /// <summary>
    /// Has the objective been completed?
    /// </summary>
    [DataField]
    public bool Completed;

    /// <summary>
    /// The range to check for <see cref="DrunkInBarTargetComponent"/> entities.
    /// </summary>
    [DataField]
    public float Range = 10f;
}
