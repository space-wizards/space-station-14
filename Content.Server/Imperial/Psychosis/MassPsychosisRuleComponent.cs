using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MassPsychosisRule))]
public sealed partial class MassPsychosisRuleComponent : Component
{
    [DataField("from")]
    public int From = 0;
    [DataField("to")]
    public int To = 0;
}
