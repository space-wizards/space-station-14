using Content.Server.StationEvents;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(SurvivalRuleSystem))]
public sealed partial class SurvivalRuleComponent : Component
{
    [DataField("endTime"), ViewVariables(VVAccess.ReadWrite)]
    public float EndTime;

    [DataField("maxChaos"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxChaos;

    [DataField("startingChaos"), ViewVariables(VVAccess.ReadWrite)]
    public float StartingChaos;

    [DataField("timeUntilNextEvent"), ViewVariables(VVAccess.ReadWrite)]
    public float TimeUntilNextEvent;
}
