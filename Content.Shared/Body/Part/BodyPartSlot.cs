using Content.Shared.Body.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Body.Part;

[Serializable, NetSerializable]
[Access(typeof(SharedBodySystem))]
[DataDefinition]
public sealed partial record BodyPartSlot
{
    [DataField("id")]
    public string Id = string.Empty;

    [DataField("type")]
    public BodyPartType? Type;

    [NonSerialized]
    [DataField("parent")]
    public EntityUid Parent;

    public NetEntity NetParent;

    [NonSerialized]
    [DataField("child")]
    public EntityUid? Child;

    public NetEntity? NetChild;

    public void SetChild(EntityUid? child, NetEntity? netChild)
    {
        Child = child;
        NetChild = netChild;
    }

    // Rider doesn't suggest explicit properties during deconstruction without this
    public void Deconstruct(out EntityUid? child, out string id, out EntityUid parent, out BodyPartType? type)
    {
        child = Child;
        id = Id;
        parent = Parent;
        type = Type;
    }
}
