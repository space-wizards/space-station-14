using Robust.Shared.GameStates;

namespace Content.Shared.Containers.ItemSlots.Components;

/// <summary>
/// Activating a UI with this component requires the <seealso cref="ItemSlotsComponent"/>
/// and having at least one of the item slots filled.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ItemSlotsSystem))]
public sealed partial class ActivatableUIRequiresFilledItemSlotComponent : Component;
