using Robust.Shared.GameStates;

namespace Content.Shared.Slippery;

/// <summary>
/// Applies continuous movement to the attached entity when colliding with super slipper entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlidingComponent : Component
{
    /// <summary>
    ///     The friction modifier that will be applied to any friction calculations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FrictionModifier;

    /// <summary>
    /// Hashset of contacting entities.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Contacting = new();
}
