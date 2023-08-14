using Content.Shared.Body.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Organ;

[Serializable, NetSerializable]
[Access(typeof(SharedBodySystem))]
[DataRecord]
public sealed record OrganSlot(string Id)
{
    [NonSerialized]
    public EntityUid Parent;

    public NetEntity NetParent;

    [NonSerialized] public EntityUid? Child;

    public NetEntity? NetChild;

    // Rider doesn't suggest explicit properties during deconstruction without this
    public void Deconstruct(out EntityUid? child, out string id, out EntityUid parent)
    {
        child = Child;
        id = Id;
        parent = Parent;
    }
}
