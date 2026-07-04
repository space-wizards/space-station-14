using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// This is used for items that need
/// multiple hands to be able to be picked up
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MultiHandedItemComponent : Component
{
    /// <summary>
    /// Number of free hands required to hold the item.
    /// </summary>
    [DataField]
    public int HandsNeeded = 2;
}
