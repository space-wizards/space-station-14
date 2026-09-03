using Robust.Shared.GameStates;

namespace Content.Shared.Inventory;

/// <summary>
/// Makes so a item doesn't show the item name in the hand UI
/// Also centers the vertical alignment of the item status panel
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockItemNameInHandComponent : Component;

