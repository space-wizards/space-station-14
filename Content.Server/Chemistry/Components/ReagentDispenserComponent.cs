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

        [DataField("pack", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentDispenserInventoryPrototype>))]
        [ViewVariables(VVAccess.ReadWrite)]
        public string? PackPrototypeId = default!;

        [DataField("emagPack", customTypeSerializer:typeof(PrototypeIdSerializer<ReagentDispenserInventoryPrototype>))]
        [ViewVariables(VVAccess.ReadWrite)]
        public string? EmagPackPrototypeId = default!;

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentDispenserDispenseAmount DispenseAmount = ReagentDispenserDispenseAmount.U10;
    }
}
