using Content.Shared.Body.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Organ;

[Serializable, NetSerializable]
[Access(typeof(SharedBodySystem))]
[DataRecord]
public sealed record OrganSlot(string Id, EntityUid Parent)
{
    public EntityUid? Child { get; set; }

    // Rider doesn't suggest explicit properties during deconstruction without this
    public void Deconstruct(out EntityUid? child, out string id, out EntityUid parent)
    {
        child = Child;
        id = Id;
        parent = Parent;
    }
}
