using System;
using System.Collections.Generic;
using Content.Shared.Cargo;
using Content.Shared.Sound;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Cargo.Components
{

    //This entire class is a PLACEHOLDER for the cargo shuttle.
    //welp only need auto-docking now.

    [RegisterComponent, Friend(typeof(CargoSystem))]
    public sealed class CargoTelepadComponent : Component
    {
        [DataField("duration")]
        public float Duration;

        [DataField("delay")]
        public float Delay = 15f;

        [ViewVariables]
        public bool Enabled = true;

        /// <summary>
        /// How much time we've accumulated until next teleport.
        /// </summary>
        [ViewVariables]
        public float Accumulator = 0f;

        public readonly Stack<CargoOrderData> TeleportQueue = new();

        public CargoTelepadState CurrentState = CargoTelepadState.Unpowered;

        [DataField("teleportSound")] public SoundSpecifier TeleportSound = new SoundPathSpecifier("/Audio/Machines/phasein.ogg");

        /// <summary>
        ///     The paper-type prototype to spawn with the order information.
        /// </summary>
        [DataField("printerOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PrinterOutput = "Paper";
    }

    public enum CargoTelepadState : byte
    {
        Unpowered,
        Idle,
        Charging,
        Teleporting,
    };
}
