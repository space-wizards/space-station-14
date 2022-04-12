using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Sound;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cargo.Components
{
    /// <summary>
    /// Handles teleporting in requested cargo after the specified delay.
    /// </summary>
    [RegisterComponent, Friend(typeof(CargoSystem))]
    public sealed class CargoTelepadComponent : SharedCargoTelepadComponent
    {
        [DataField("delay")]
        public float Delay = 20f;

        /// <summary>
        /// How much time we've accumulated until next teleport.
        /// </summary>
        [ViewVariables]
        public float Accumulator = 0f;

        [ViewVariables]
        public readonly Stack<CargoOrderData> TeleportQueue = new();

        [ViewVariables]
        public CargoTelepadState CurrentState = CargoTelepadState.Unpowered;

        [DataField("teleportSound")] public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Machines/phasein.ogg");

        /// <summary>
        ///     The paper-type prototype to spawn with the order information.
        /// </summary>
        [DataField("printerOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PrinterOutput = "Paper";
    }
}
