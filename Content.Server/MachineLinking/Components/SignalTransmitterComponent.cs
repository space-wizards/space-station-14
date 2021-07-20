using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public class SignalTransmitterComponent : Component
    {
        public override string Name => "SignalTransmitter";

        [DataField("outputs")]
        private List<SignalPort> _outputs = new();

        [ViewVariables]
        public IReadOnlyList<SignalPort> Outputs => _outputs;

        public List<(SignalReceiverComponent transmitter, string receiverPort, string ourPort)> Connections = new();
    }
}
