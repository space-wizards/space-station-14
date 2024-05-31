using Content.Shared.Whitelist;
using Content.Shared.Containers.ItemSlots;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Dispenser;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// A machine that dispenses reagents into a solution container from containers in its storage slots.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ReagentDispenserSystem))]
    public sealed partial class ReagentDispenserComponent : Component
    {
        /// <summary>
        /// String with the pack name that stores the initial fill of the dispenser. The initial
        /// fill is added to the dispenser on MapInit. Note that we don't use ContainerFill because
        /// we have to generate the storage slots at MapInit first, then fill them.
        /// </summary>
        [DataField("pack", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentDispenserInventoryPrototype>))]
        [ViewVariables(VVAccess.ReadWrite)]
        public string? PackPrototypeId = default!;

        /// <summary>
        /// Maximum number of internal storage slots. Dispenser can't store (or dispense) more than
        /// this many chemicals (without unloading and reloading).
        /// </summary>
        [DataField("numStorageSlots")]
        public int NumSlots = 25;

        /// <summary>
        /// For each created storage slot for the reagent containers being dispensed, apply this
        /// entity whitelist. Makes sure weird containers don't fit in the dispenser and that beakers
        /// don't accidentally get slotted into the source slots.
        /// </summary>
        [DataField]
        public EntityWhitelist? StorageWhitelist;

        [DataField]
        public ItemSlot BeakerSlot = new();

        /// <summary>
        /// Prefix for automatically-generated slot name for storage, up to NumSlots.
        /// </summary>
        public static string BaseStorageSlotId = "ReagentDispenser-storageSlot";

        /// <summary>
        /// List of storage slots that were created at MapInit.
        /// </summary>
        [DataField]
        public List<string> StorageSlotIds = new List<string>();

        [DataField]
        public List<ItemSlot> StorageSlots = new List<ItemSlot>();

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentDispenserDispenseAmount DispenseAmount = ReagentDispenserDispenseAmount.U10;
    }
}
