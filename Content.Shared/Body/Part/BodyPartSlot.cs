using Content.Shared.Body.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part;

[Serializable, NetSerializable]
[Access(typeof(SharedBodySystem))]
[DataRecord]
public sealed record BodyPartSlot(string Id, EntityUid Parent, BodyPartType? Type)
{
    public EntityUid? Child { get; set; }
}
