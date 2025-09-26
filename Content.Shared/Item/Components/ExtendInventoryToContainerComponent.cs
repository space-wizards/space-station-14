using Robust.Shared.GameStates;

namespace Content.Shared.Item.Components;

/// <summary>
///     If inserted into an Itemslot, this entity will extend its inventory to the container.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExtendInventoryToContainerComponent : Component;
