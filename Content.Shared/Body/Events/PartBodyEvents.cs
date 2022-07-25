using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Events;

/// <summary>
///     Raised on a body part and the body when added to a body.
/// </summary>
[Serializable, NetSerializable]
public sealed class PartAddedToBodyEvent : EntityEventArgs
{
    public readonly EntityUid BodyUid;
    public readonly EntityUid BodyPartUid;
    public readonly string SlotId;

    public PartAddedToBodyEvent(EntityUid bodyUid, EntityUid bodyPartUid, string slotId)
    {
        BodyUid = bodyUid;
        BodyPartUid = bodyPartUid;
        SlotId = slotId;
    }
}

/// <summary>
///     Raised on a body part and the body when removed from a body.
/// </summary>
[Serializable, NetSerializable]
public sealed class PartRemovedFromBodyEvent : EntityEventArgs
{
    public readonly EntityUid BodyUid;
    public readonly EntityUid BodyPartUid;
    public readonly string SlotId;

    public PartRemovedFromBodyEvent(EntityUid bodyUid, EntityUid bodyPartUid, string slotId)
    {
        BodyUid = bodyUid;
        BodyPartUid = bodyPartUid;
        SlotId = slotId;
    }
}
