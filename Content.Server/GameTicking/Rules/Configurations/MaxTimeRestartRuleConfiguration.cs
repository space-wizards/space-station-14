using JetBrains.Annotations;

namespace Content.Server.GameTicking.Rules.Configurations;

/// <summary>
/// Configures the <see cref="InactivityTimeRestartRuleSystem"/> game rule.
/// </summary>
[UsedImplicitly]
public sealed class MaxTimeRestartRuleConfiguration : GameRuleConfiguration
{
    public override string Id => "MaxTimeRestart"; // The value for this in the system isn't static and can't be made static. RIP.

    [DataField("roundMaxTime", required: true)]
    public TimeSpan RoundMaxTime { get; }
    [DataField("roundEndDelay", required: true)]
    public TimeSpan RoundEndDelay { get; }
}
