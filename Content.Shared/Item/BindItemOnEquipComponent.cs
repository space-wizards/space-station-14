using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// Raises a <see cref="BindItemEvent"/> on the wearer when this item is equipped.
/// This allows systems on the wearer to keep a reference to the equipped item.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BindItemOnEquipComponent : Component;
