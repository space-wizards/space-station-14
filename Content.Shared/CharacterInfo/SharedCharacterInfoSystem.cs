using Content.Shared.Objectives;
using Robust.Shared.Serialization;

namespace Content.Shared.CharacterInfo;

[Serializable, NetSerializable]
public sealed class RequestCharacterInfoEvent : EntityEventArgs
{
    public readonly EntityUid EntityUid;

    public RequestCharacterInfoEvent(EntityUid entityUid)
    {
        EntityUid = entityUid;
    }
}

[Serializable, NetSerializable]
public sealed class CharacterInfoEvent : EntityEventArgs
{
    public readonly EntityUid EntityUid;
    public readonly string JobTitle;
    public readonly Dictionary<string, List<ObjectiveInfo>> Objectives;
    public readonly string? Briefing;

    public CharacterInfoEvent(EntityUid entityUid, string jobTitle, Dictionary<string, List<ObjectiveInfo>> objectives, string? briefing)
    {
        EntityUid = entityUid;
        JobTitle = jobTitle;
        Objectives = objectives;
        Briefing = briefing;
    }
}
