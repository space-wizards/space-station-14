using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.MachineLinking
{
    [Serializable, NetSerializable]
    public sealed class SignalPortsState : BoundUserInterfaceState
    {
        public readonly string TransmitterName;
        /// <summary>
        /// A List of all ports on the selected transmitter
        /// </summary>
        public readonly List<string> TransmitterPorts;

        public readonly string ReceiverName;
        /// <summary>
        /// A List of all ports on the selected receiver
        /// </summary>
        public readonly List<string> ReceiverPorts;

        public SignalPortsState(string transmitterName, List<string> transmitterPorts, string receiverName, List<string> receiverPorts)
        {
            TransmitterName = transmitterName;
            TransmitterPorts = transmitterPorts;
            ReceiverName = receiverName;
            ReceiverPorts = receiverPorts;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SignalPortSelected : BoundUserInterfaceMessage
    {
        public readonly string TransmitterPort;
        public readonly string ReceiverPort;

        public SignalPortSelected(string transmitterPort, string receiverPort)
        {
            TransmitterPort = transmitterPort;
            ReceiverPort = receiverPort;
        }
    }
}
