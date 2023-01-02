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
    /// A machine that dispenses reagents into a solution container.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ReagentDispenserSystem))]
    public sealed partial class ReagentDispenserComponent : Component
    {

        //packs are used for presets
        [DataField("pack", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentDispenserInventoryPrototype>))]
        [ViewVariables(VVAccess.ReadWrite)]
        public string? PackPrototypeId = default!;

        [DataField("numStorageSlots")]
        public int NumSlots = 25;

        [DataField("storageWhitelist")]
        public EntityWhitelist? StorageWhitelist;

        public static string BeakerSlotId = "ReagentDispenser-beakerSlot";
        public static string BaseStorageSlotId = "ReagentDispenser-storageSlot";
        public List<string> StorageSlotIds = new List<string>();

        [DataField("beakerSlot")]
        public ItemSlot BeakerSlot = new();

        [DataField("storageSlots")]
        public List<ItemSlot> StorageSlots = new List<ItemSlot>();

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentDispenserDispenseAmount DispenseAmount = ReagentDispenserDispenseAmount.U10;
    }
}
