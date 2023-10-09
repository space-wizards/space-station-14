using Content.Shared.Objectives;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterInfo;

[Serializable, NetSerializable]
public sealed class RequestAntagonistInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;

    public RequestAntagonistInfoEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

[Serializable, NetSerializable]
public sealed class AntagonistInfoEvent : EntityEventArgs
{
    public readonly NetEntity NetEntity;
    public readonly NetEntity AntagonistNetEntity;
    public readonly string JobTitle;
    public readonly Dictionary<string, List<ObjectiveInfo>> Objectives;

    public AntagonistInfoEvent(NetEntity netEntity, NetEntity antagonistNetEntity, string jobTitle, Dictionary<string, List<ObjectiveInfo>> objectives)
    {
        NetEntity = netEntity;
        AntagonistNetEntity = antagonistNetEntity;
        JobTitle = jobTitle;
        Objectives = objectives;
    }
}
