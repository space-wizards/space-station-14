using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Clothing that contributes to the wearer's examine message when worn
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ExaminableClothingComponent : Component
{
    /// <summary>
    /// Localization ID of the examine text to display. It will be slotted into a sentence of the form "PRONOUN(Wearer) is wearing INDEFINITE(ExaminableClothing)." If unspecified, defaults to the item's name.
    /// </summary>
    [DataField]
    public LocId? ExamineText = null;

    /// <summary>
    /// Only adds the examine text in the given slots.
    /// </summary>
    [DataField]
    public SlotFlags AllowedSlots = SlotFlags.WITHOUT_POCKET;
}
