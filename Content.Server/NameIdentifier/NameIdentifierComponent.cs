using Content.Shared.NameIdentifier;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.NameIdentifier;

[RegisterComponent]
public sealed class NameIdentifierComponent : Component
{
    [DataField("group", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<NameIdentifierGroupPrototype>))]
    public string Group = string.Empty;
}
