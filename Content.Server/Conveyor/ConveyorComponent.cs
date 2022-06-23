using Content.Shared.Conveyor;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Conveyor
{
    [RegisterComponent]
    [Access(typeof(ConveyorSystem))]
    public sealed class ConveyorComponent : Component
    {
        /// <summary>
        ///     The angle to move entities by in relation to the owner's rotation.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("angle")]
        public Angle Angle = Angle.Zero;

        /// <summary>
        ///     The amount of units to move the entity by per second.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("speed")]
        public float Speed = 2f;

        /// <summary>
        ///     The current state of this conveyor
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public ConveyorState State;

        [DataField("forwardPort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
        public string ForwardPort = "Forward";

        [DataField("reversePort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string ReversePort = "Reverse";

        [DataField("offPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string OffPort = "Off";
    }
}
