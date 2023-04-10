using Content.Server.Cargo.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.MachineLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cargo.Components
{
    /// <summary>
    /// Handles teleporting in requested cargo after the specified delay.
    /// </summary>
    [RegisterComponent, Access(typeof(CargoSystem))]
    public sealed class CargoTelepadComponent : SharedCargoTelepadComponent
    {
        [DataField("delay")]
        public float Delay = 45f;

        /// <summary>
        /// How much time we've accumulated until next teleport.
        /// </summary>
        [DataField("accumulator")]
        public float Accumulator = 0f;

        [ViewVariables]
        public CargoTelepadState CurrentState = CargoTelepadState.Unpowered;

        [DataField("teleportSound")] public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Machines/phasein.ogg");

        /// <summary>
        ///     The paper-type prototype to spawn with the order information.
        /// </summary>
        [DataField("printerOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PrinterOutput = "PaperCargoInvoice";

        [DataField("receiverPort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
        public string ReceiverPort = "OrderReceiver";
    }
}
