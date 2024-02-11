namespace Content.Client.Inventory;

/// <summary>
/// A character UI which shows items the user has equipped within his inventory
/// </summary>
[RegisterComponent]
[Access(typeof(ClientInventorySystem))]
public sealed partial class InventorySlotsComponent : Component
{
    [ViewVariables]
    public readonly Dictionary<string, ClientInventorySystem.SlotData> SlotData = new ();

    /// <summary>
    ///     Data about the current layers that have been added to the players sprite due to the items in each equipment slot.
    /// </summary>
    [ViewVariables]
    [Access(typeof(ClientInventorySystem), Other = AccessPermissions.ReadWriteExecute)] // FIXME Friends
    public readonly Dictionary<string, HashSet<string>> VisualLayerKeys = new();
}
