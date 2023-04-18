using System.Threading;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent]
public sealed class InactivityRuleComponent : Component
{
    [DataField("inactivityMaxTime", required: true)]
    public TimeSpan InactivityMaxTime = TimeSpan.FromMinutes(10);

    [DataField("roundEndDelay", required: true)]
    public TimeSpan RoundEndDelay  = TimeSpan.FromSeconds(10);

    public CancellationTokenSource TimerCancel = new();
}
