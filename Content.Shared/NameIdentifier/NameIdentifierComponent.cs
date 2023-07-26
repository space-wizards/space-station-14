using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.NameIdentifier;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NameIdentifierComponent : Component
{
    [DataField("group", required: true, customTypeSerializer:typeof(PrototypeIdSerializer<NameIdentifierGroupPrototype>))]
    public string Group = string.Empty;

    /// <summary>
    /// The randomly generated ID for this entity.
    /// </summary>
    [DataField("identifier"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int Identifier = -1;

    /// <summary>
    /// The full name identifier for this entity.
    /// </summary>
    [DataField("fullIdentifier"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string FullIdentifier = string.Empty;
}
