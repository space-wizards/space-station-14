using Robust.Shared.Network;

namespace Content.Server.GameTicking.Replays;

[Serializable, DataDefinition]
public partial struct ReplayEventPlayer
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

    public NetEntity? PlayerNetEntity;

    [DataField]
    public bool? Antag;
}
