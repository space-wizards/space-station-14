using System.Threading;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Configures the <see cref="InactivityTimeRestartRuleSystem"/> game rule.
/// </summary>
[RegisterComponent]
public sealed class MaxTimeRestartRuleComponent : Component
{
    public CancellationTokenSource _timerCancel = new();

    [DataField("roundMaxTime", required: true)]
    public TimeSpan RoundMaxTime = TimeSpan.FromMinutes(5);
    [DataField("roundEndDelay", required: true)]
    public TimeSpan RoundEndDelay = TimeSpan.FromSeconds(10);
}
