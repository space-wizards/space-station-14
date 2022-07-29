using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.VendingMachines
{
    [Serializable, NetSerializable, Prototype("vendingMachineInventory")]
    public sealed class VendingMachineInventoryPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("name")]
        public string Name { get; } = string.Empty;

        [DataField("animationDuration")]
        public double AnimationDuration { get; }

        // TODO make this a proper sprite specifier for yaml linting.
        [DataField("spriteName")]
        public string SpriteName { get; } = string.Empty;

        [DataField("startingInventory")]
        public Dictionary<string, uint> StartingInventory { get; } = new();

        [DataField("emaggedInventory")]
        public Dictionary<string, uint>? EmaggedInventory { get; }

        [DataField("contrabandInventory")]
        public Dictionary<string, uint>? ContrabandInventory { get; }
    }
}
