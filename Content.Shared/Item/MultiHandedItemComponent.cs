using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// This is used for items that need
/// multiple hands to be able to be picked up
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class MultiHandedItemComponent : Component
{
    [DataField("handsNeeded"), ViewVariables(VVAccess.ReadWrite)]
    public int HandsNeeded = 2;
}
