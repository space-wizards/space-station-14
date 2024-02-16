namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// This is used for a rule that announces kills globally.
/// </summary>
[RegisterComponent, Access(typeof(KillCalloutRuleSystem))]
public sealed partial class KillCalloutRuleComponent : Component
{
    /// <summary>
    /// Root used to generate kill callouts
    /// </summary>
    [DataField("killCalloutPrefix")]
    public string KillCalloutPrefix = "death-match-kill-callout-";

    /// <summary>
    /// A value used to randomly select a kill callout
    /// </summary>
    [DataField("killCalloutAmount")]
    public int KillCalloutAmount = 60;

    /// <summary>
    /// Root used to generate kill callouts when a player is killed by the environment
    /// </summary>
    [DataField("environmentKillCallouts")]
    public string SelfKillCalloutPrefix = "death-match-kill-callout-env-";

    /// <summary>
    /// A value used to randomly select a kill callout when a player is killed by the environment
    /// </summary>
    [DataField("selfKillCalloutAmount")]
    public int SelfKillCalloutAmount = 10;
}
