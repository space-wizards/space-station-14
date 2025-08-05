using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Rotatable;

/// <summary>
/// Allows an entity to be flipped (mirrored) by using a verb.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlippableComponent : Component
{
    /// <summary>
    /// Entity to replace this entity with when the current one is 'flipped'.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId MirrorEntity = default!;
}
