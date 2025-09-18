using Robust.Shared.GameStates;

namespace Content.Shared.Bible.Components;

/// <summary>
/// This component is for the chaplain's familiars, and mostly
/// used to track their current state and to give a component to check for
/// if any special behavior is needed.
/// </summary>
/// <remarks>
/// Important! This component is intentionally not networked. Do not add
/// <see cref="NetworkedComponentAttribute"/>; otherwise other players/clients could 
/// infer that the entity is a familiar and who it belongs to (metagaming).
/// </remarks>
[RegisterComponent]
public sealed partial class FamiliarComponent : Component
{
    /// <summary>
    /// The entity this familiar was summoned from.
    /// </summary>
    [DataField]
    public EntityUid? Source = null;
}
