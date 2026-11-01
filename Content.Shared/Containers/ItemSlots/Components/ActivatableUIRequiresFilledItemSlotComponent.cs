using Robust.Shared.GameStates;

namespace Content.Shared.Containers.ItemSlots.Components;

/// <summary>
/// Activating a UI with this component requires the <seealso cref="ItemSlotsComponent"/>
/// and having at least one of the item slots filled.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ItemSlotsSystem))]
public sealed partial class ActivatableUIRequiresFilledItemSlotComponent : Component
{
    /// <summary>
    /// Whether to condense item slots containing the same item prototype in the same radial menu option.
    /// This is used in RadialItemSlotMenuBoundUserInterface.
    /// </summary>
    /// <remarks>
    /// If, for example, two slots contain a wet floor sign, they will be condensed into one nested radial option, instead of two radial options.
    /// </remarks>
    [DataField]
    public bool CompactSimilarItemsInRadialMenu = true;
}
