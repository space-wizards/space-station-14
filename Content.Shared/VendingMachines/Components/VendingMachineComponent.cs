using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.VendingMachines.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class VendingMachineComponent : Component
{
    /// <summary>
    /// PrototypeID for the vending machine's inventory, see <see cref="VendingMachineInventoryPrototype"/>
    /// </summary>
    [DataField("pack", required: true)]
    public ProtoId<VendingMachineInventoryPrototype> PackPrototypeId;

    [DataField, AutoNetworkedField]
    public Dictionary<string, VendingMachineInventoryEntry> Inventory = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, VendingMachineInventoryEntry> EmaggedInventory = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, VendingMachineInventoryEntry> ContrabandInventory = new();

    /// <summary>
    /// If true then unlocks the <see cref="ContrabandInventory"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Contraband;

    [DataField, AutoNetworkedField]
    public bool Broken;

    /// <summary>
    /// The quality of the stock in the vending machine on spawn.
    /// Represents the percentage chance (0.0f = 0%, 1.0f = 100%) each set of items in the machine is fully-stocked.
    /// If not fully stocked, the stock will have a random value between 0 (inclusive) and max stock (exclusive).
    /// </summary>
    [DataField]
    public float InitialStockQuality = 1.0f;

    /// <summary>
    /// Audio entity used during restock in case the doafter gets canceled.
    /// </summary>
    [DataField]
    public EntityUid? RestockStream;
}

public sealed partial class VendingMachineSelfDispenseEvent : InstantActionEvent;
