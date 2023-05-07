using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.System;

namespace Content.Server.MachineLinking.Components
{
    [DataDefinition]
    public readonly struct PortIdentifier
    {
        [DataField("uid")]
        public readonly EntityUid Uid;

        [DataField("port")]
        public readonly string Port;

        public PortIdentifier(EntityUid uid, string port)
        {
            Uid = uid;
            Port = port;
        }
    }

    [RegisterComponent]
    [Access(typeof(SignalLinkerSystem))]
    public sealed class SignalTransmitterComponent : Component
    {
        /// <summary>
        ///     How far the device can transmit a signal wirelessly.
        ///     Devices farther than this range can still transmit if they are
        ///     on the same powernet.
        /// </summary>
        [DataField("transmissionRange")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float TransmissionRange = 30f;

        /*
         * Remember last output state to avoid re-raising a SignalChangedEvent if the signal
         * level hasn't actually changed.
         */
        [ViewVariables(VVAccess.ReadWrite)]
        public SignalState LastState = SignalState.Low;

        [DataField("outputs")]
        [Access(typeof(SignalLinkerSystem), Other = AccessPermissions.ReadExecute)] // FIXME Friends
        public Dictionary<string, List<PortIdentifier>> Outputs = new();
    }
}
