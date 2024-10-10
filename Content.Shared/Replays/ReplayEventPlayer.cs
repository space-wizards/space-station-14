using Robust.Shared.Network;

namespace Content.Shared.Replays;

[Serializable, DataDefinition]
public partial class ReplayEventPlayer
{
    [DataField]
    public string? PlayerOOCName;

    [DataField]
    public string? PlayerICName;

    [DataField, NonSerialized]
    public NetUserId? PlayerGuid;

    [DataField, NonSerialized]
    public string[]? JobPrototypes;

    [DataField, NonSerialized]
    public string[]? AntagPrototypes;
}
