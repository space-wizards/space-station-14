using Content.Server.MachineLinking.System;

namespace Content.Server.MachineLinking.Components
{
    [DataDefinition]
    public struct PortIdentifier
    {
        [DataField("uid")]
        public EntityUid Uid;

        [DataField("port")]
        public string Port;

        public PortIdentifier(EntityUid uid, string port)
        {
            Uid = uid;
            Port = port;
        }
    }

    [RegisterComponent]
    [Friend(typeof(SignalLinkerSystem))]
    public sealed class SignalTransmitterComponent : Component
    {
        /// <summary>
        ///     How far the device can transmit a signal wirelessly.
        ///     Devices farther than this range can still transmit if they are
        ///     on the same powernet.
        /// </summary>
        [DataField("transmissionRange")]
        public float TransmissionRange = 30f;

        [DataField("outputs")]
        public Dictionary<string, List<PortIdentifier>> Outputs = new();
    }
}
