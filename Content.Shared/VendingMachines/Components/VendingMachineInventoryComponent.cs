using Content.Shared.Actions.ActionTypes;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.VendingMachines.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VendingMachineInventoryComponent : Component
{
    /// <summary>
    /// PrototypeID for the vending machine's inventory, see <see cref="VendingMachineInventoryPrototype"/>
    /// </summary>
    [DataField("pack",
        customTypeSerializer: typeof(PrototypeIdListSerializer<VendingMachineInventoryPrototype>), required: true)]
    public List<string> PackPrototypeId = new();

    /// <summary>
    ///     The action available to the player controlling the vending machine
    /// </summary>
    [DataField("action", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string? Action = "VendingThrow";

    /// <summary>
    ///
    /// </summary>
    [DataField("whiteList")]
    public EntityWhitelist? Whitelist = null;

    [ViewVariables]
    public Dictionary<string, List<VendingMachineInventoryEntry>> Items = new();

    public Container? Storage;

    public bool Contraband;
}
