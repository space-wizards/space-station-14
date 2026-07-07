using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.NameIdentifier;

/// <summary>
/// Generates a unique numeric identifier for entities, with specifics controlled by a <see cref="NameIdentifierGroupPrototype"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NameIdentifierComponent : Component
{
    [DataField]
    public ProtoId<NameIdentifierGroupPrototype>? Group;

    /// <summary>
    /// The randomly generated ID for this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Identifier = -1;

    /// <summary>
    /// The full name identifier for this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string FullIdentifier = string.Empty;
}
