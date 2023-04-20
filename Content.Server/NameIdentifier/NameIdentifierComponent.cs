using Content.Shared.NameIdentifier;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.NameIdentifier;

[RegisterComponent]
public sealed class NameIdentifierComponent : Component
{
    [DataField("group", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<NameIdentifierGroupPrototype>))]
    public string Group = string.Empty;

    /// <summary>
    /// The randomly generated ID for this entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("identifier")]
    public int Identifier = -1;
}
