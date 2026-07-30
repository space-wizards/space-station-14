using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// Asks an entity to bind this item when it is equipped.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BindItemOnEquipComponent : Component;
