using Content.Shared.Actions.ActionTypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.VendingMachines.Components;

[RegisterComponent, NetworkedComponent]
public sealed class VendingMachineInventoryComponent : Component
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

    [ViewVariables]
    public Dictionary<string, List<VendingMachineInventoryEntry>> Items = new();

    public bool Contraband;
}
