namespace Content.Shared.PAI;

/// <summary>
/// Lets an expansion card be installed to this pAI.
/// Requires an item slot to exist.
/// </summary>
[RegisterComponent, Access(typeof(PAIExpansionSystem))]
public sealed partial class PAIExpansionSlotComponent: Component
{
    /// <summary>
    /// Name of the item slot holding an expansion card.
    /// </summary>
    [DataField]
    public string SlotId = "expansion";

    /// <summary>
    /// Popup shown when trying to insert a card when the panel is closed.
    /// </summary>
    [DataField]
    public LocId PanelClosedPopup = "pai-expansion-slot-closed";
}
