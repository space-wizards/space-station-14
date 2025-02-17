using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component for the RevolutionaryRuleSystem that stores info about winning/losing, player counts required for starting, as well as prototypes for Revolutionaries and their gear.
/// </summary>
[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]
public sealed partial class RevolutionaryRuleComponent : Component
{
    /// <summary>
    /// When will the percentage of revolutionaries and the living command be checked.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Check;

    /// <summary>
    /// The amount of time between each check.
    /// </summary>
    [DataField]
    public TimeSpan TimerWait = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The time it takes after the last head is killed for the shuttle to arrive.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ShuttleCallTime = TimeSpan.FromMinutes(5);

    public RevolutionaryStage Stage = RevolutionaryStage.Initial;
}

public enum RevolutionaryStage : byte
{
    Initial,
    Massacre
}
