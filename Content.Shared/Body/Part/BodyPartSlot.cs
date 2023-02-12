using Content.Shared.Body.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part;

[Serializable, NetSerializable]
[Access(typeof(SharedBodySystem))]
[DataRecord]
public sealed record BodyPartSlot(string Id, EntityUid Parent, BodyPartType? Type)
{
    public EntityUid? Child { get; set; }

    // Rider doesn't suggest explicit properties during deconstruction without this
    public void Deconstruct(out EntityUid? child, out string id, out EntityUid parent, out BodyPartType? type)
    {
        child = Child;
        id = Id;
        parent = Parent;
        type = Type;
    }
}
