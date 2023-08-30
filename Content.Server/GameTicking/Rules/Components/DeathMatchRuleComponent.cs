namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
///     Simple GameRule that will do a free-for-all death match.
///     Kill everybody else to win.
/// </summary>
[RegisterComponent, Access(typeof(DeathMatchRuleSystem))]
public sealed partial class DeathMatchRuleComponent : Component
{
    /// <summary>
    /// How long until the round restarts
    /// </summary>
    [DataField("restartDelay"), ViewVariables(VVAccess.ReadWrite)]
    public float RestartDelay = 10f;

    /// <summary>
    /// How long after a person dies will the restart be checked
    /// </summary>
    [DataField("deadCheckDelay"), ViewVariables(VVAccess.ReadWrite)]
    public float DeadCheckDelay = 5f;

    /// <summary>
    /// A timer for checking after a death
    /// </summary>
    [DataField("deadCheckTimer"), ViewVariables(VVAccess.ReadWrite)]
    public float? DeadCheckTimer;

    /// <summary>
    /// A timer for the restart.
    /// </summary>
    [DataField("restartTimer"), ViewVariables(VVAccess.ReadWrite)]
    public float? RestartTimer;
}
