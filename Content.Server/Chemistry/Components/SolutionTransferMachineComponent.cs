using Content.Shared.Whitelist;
using Content.Shared.Containers.ItemSlots;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Dispenser;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// A machine that dispenses reagents into a solution container from containers in its storage slots.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(SolutionTransferMachineSystem))]
    public sealed partial class SolutionTransferMachineComponent : Component
    {
        /// <summary>
        /// String with the pack name that stores the initial fill of the dispenser. The initial
        /// fill is added to the dispenser on MapInit. Note that we don't use ContainerFill because
        /// we have to generate the storage slots at MapInit first, then fill them.
        /// </summary>
        [DataField("pack", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentDispenserInventoryPrototype>))]
        [ViewVariables(VVAccess.ReadWrite)]
        public string? PackPrototypeId = default!;

        /// <summary>
        /// Maximum number of internal storage slots. Machines can't store (or dispense) more than
        /// this many chemicals (without unloading and reloading).
        /// </summary>
        [DataField]
        public int MaxStorageSlots = 25;
        /// <summary>
        /// List of storage slots that were created at MapInit.
        /// </summary>
        [DataField]
        public List<string> StorageSlotIds = [];
        [DataField]
        public List<ItemSlot> StorageSlots = [];
        /// <summary>
        /// For each created storage slot for the reagent containers being dispensed, apply this
        /// entity whitelist. Makes sure weird containers don't fit in the dispenser and that beakers
        /// don't accidentally get slotted into the source slots.
        /// </summary>
        [DataField]
        public EntityWhitelist? StorageWhitelist;

        /// <summary>
        /// Whether to allow cherry-picking specific reagents when moving solutions around into/out of containers
        /// More specific checks can be done by whatever uses this system
        /// </summary>
        [DataField]
        public bool AllowFiltering = false;

        /// <summary>
        /// Beakers for input/output operations of the machine. Created the same way storage slots do.
        /// </summary>
        [DataField]
        public int MaxDispenserSlots = 1;
        [DataField]
        public List<string> DispenserSlotIds = [];
        [DataField]
        public List<ItemSlot> DispenserContainerSlots = [];
        [DataField]
        public EntityWhitelist? DispenserWhitelist;
    }
}