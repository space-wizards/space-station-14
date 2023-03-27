using Content.Shared.Tag;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Clothing.Components;

// modified from simplestation14
[RegisterComponent]
public sealed class ClothingGrantTagComponent : Component
{
    [DataField("tag", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<TagPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Tag = "";

    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsActive = false;
}
