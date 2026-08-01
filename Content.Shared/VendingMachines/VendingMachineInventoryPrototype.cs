using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.VendingMachines;

[Prototype]
public sealed partial class VendingMachineInventoryPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(customTypeSerializer:typeof(PrototypeIdDictionarySerializer<uint, EntityPrototype>))]
    public Dictionary<string, uint> StartingInventory { get; private set; } = new();

    [DataField(customTypeSerializer:typeof(PrototypeIdDictionarySerializer<uint, EntityPrototype>))]
    public Dictionary<string, uint>? EmaggedInventory { get; private set; }

    [DataField(customTypeSerializer:typeof(PrototypeIdDictionarySerializer<uint, EntityPrototype>))]
    public Dictionary<string, uint>? ContrabandInventory { get; private set; }
}
