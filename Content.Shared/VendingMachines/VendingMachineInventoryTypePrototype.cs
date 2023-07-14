using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines;

[Serializable, NetSerializable, Prototype("vendingMachineInventoryType")]
public sealed class VendingMachineInventoryTypePrototype: IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;
}
