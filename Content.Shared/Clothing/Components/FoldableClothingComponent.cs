using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FoldableClothingComponent : Component
{
    /// <summary>
    /// Which slots does this fit into when folded?
    /// </summary>
    [DataField]
    public SlotFlags? FoldedSlots;

    /// <summary>
    /// Which slots does this fit into when unfolded?
    /// </summary>
    [DataField]
    public SlotFlags? UnfoldedSlots;


    /// <summary>
    /// What equipped prefix does this have while in folded form?
    /// </summary>
    [DataField]
    public string? FoldedEquippedPrefix;

    /// <summary>
    /// What held prefix does this have while in folded form?
    /// </summary>
    [DataField]
    public string? FoldedHeldPrefix;
}
