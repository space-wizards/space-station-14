namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
///     Simple GameRule that will do a free-for-all death match.
///     Kill everybody else to win.
/// </summary>
[RegisterComponent]
public sealed class DeathMatchRuleComponent : Component
{
    [DataField("restartDelay"), ViewVariables(VVAccess.ReadWrite)]
    public float RestartDelay = 10f;

    [DataField("deadCheckDelay"), ViewVariables(VVAccess.ReadWrite)]
    public float DeadCheckDelay = 5f;

    [DataField("deadCheckTimer"), ViewVariables(VVAccess.ReadWrite)]
    public float? DeadCheckTimer;

    [DataField("restartTimer"), ViewVariables(VVAccess.ReadWrite)]
    public float? RestartTimer;
}
