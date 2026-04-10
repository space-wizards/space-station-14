namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that requires the player to be cut into letters and packages addressed to others.
/// Requires <see cref="NumberObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent]
public sealed partial class MailFraudConditionComponent : Component
{
    /// <summary>
    /// The number of letters and packages that have been cut into since this objective was added.
    /// </summary>
    [DataField]
    public int MailFraudCommitted = 0;
}
