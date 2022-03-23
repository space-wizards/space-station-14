using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
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
        private Dictionary<string, List<PortIdentifier>> _outputs = new();

        [ViewVariables]
        public IReadOnlyDictionary<string, List<PortIdentifier>> Outputs => _outputs;

        public void AddPort(string name)
        {
            _outputs.Add(name, new());
        }
    }
}
