using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.VendingMachines.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
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
    [DataField("action", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [AutoNetworkedField]
    public string? Action = "ActionVendingThrow";

    [DataField("actionEntity")]
    [AutoNetworkedField]
    public EntityUid? ActionEntity;

    [ViewVariables]
    public Dictionary<string, List<VendingMachineInventoryEntry>> Items = new();

    public Container? Storage;

    public bool Contraband;
}
