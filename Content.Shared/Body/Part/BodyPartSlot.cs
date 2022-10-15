using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part;

[Serializable, NetSerializable]
public sealed record BodyPartSlot(string Id, EntityUid Parent, BodyPartType? Type)
{
    public EntityUid? Child { get; set; }
}
