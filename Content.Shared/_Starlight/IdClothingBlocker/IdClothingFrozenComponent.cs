using Robust.Shared.GameStates;

namespace Content.Shared._Starlight.IdClothingBlocker;

/// <summary>
/// Component put on players who are blocked due to lack of access to equipment.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IdClothingFrozenComponent : Component
{
    /// <summary>
    /// The source of this component
    /// </summary>
    [DataField, AutoNetworkedField] public EntityUid ClothingItem;
    
    /// <summary>
    /// Whether the clothing item is currently blocked from being unequipped
    /// </summary>
    [DataField, AutoNetworkedField] public bool IsBlocked = false;
}