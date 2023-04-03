using JetBrains.Annotations;

namespace Content.Server.GameTicking.Rules.Configurations;

/// <summary>
/// Configures the <see cref="InactivityTimeRestartRuleSystem"/> game rule.
/// </summary>
[UsedImplicitly]
public sealed class InactivityGameRuleConfiguration : GameRuleConfiguration
{
    public override string Id => "InactivityTimeRestart"; // The value for this in the system isn't static and can't be made static. RIP.

    [DataField("inactivityMaxTime", required: true)]
    public TimeSpan InactivityMaxTime { get; }
    [DataField("roundEndDelay", required: true)]
    public TimeSpan RoundEndDelay { get; }
}
