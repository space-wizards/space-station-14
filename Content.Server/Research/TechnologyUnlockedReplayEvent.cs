using Content.Server.GameTicking.Replays;

namespace Content.Server.Research;

[Serializable, DataDefinition]
public sealed partial class TechnologyUnlockedReplayEvent : ReplayEvent
{
    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string Discipline = string.Empty;

    [DataField]
    public int Tier;

    [DataField]
    public ReplayEventPlayer Player;
}
