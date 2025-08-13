using Robust.Shared.GameStates;

namespace Content.Shared.Wieldable.Components;

/// <summary>
/// Blocks an entity from wielding items.
/// When added to an item, it will block wielding when held in hand or equipped.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WieldingBlockerComponent : Component
{
    /// <summary>
    /// Block wielding when this item is held in a hand?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BlockInHand = true;

    /// <summary>
    /// Block wielding when this item is equipped?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BlockEquipped = true;
}
