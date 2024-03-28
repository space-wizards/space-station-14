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
}
