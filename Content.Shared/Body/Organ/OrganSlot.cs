using Content.Shared.Body.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Organ;

[Serializable, NetSerializable]
[Access(typeof(SharedBodySystem))]
[DataDefinition]
public sealed partial record OrganSlot
{
    [DataField("id")]
    public string Id = string.Empty;

    [NonSerialized]
    [DataField("parent")]
    public EntityUid Parent;

    public NetEntity NetParent;

    [NonSerialized]
    [DataField("child")]
    public EntityUid? Child;

    public NetEntity? NetChild;

    // Rider doesn't suggest explicit properties during deconstruction without this
    public void Deconstruct(out EntityUid? child, out string id, out EntityUid parent)
    {
        child = Child;
        id = Id;
        parent = Parent;
    }
}
