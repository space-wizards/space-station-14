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
        /// <summary>
        /// A Dictionary containing all ports and wether or not they can be selected.
        /// </summary>
        public readonly Dictionary<string, bool> Ports;

        public SignalPortsState(string[] ports) : this(ports.ToDictionary(s => s, _ => true))
        {
        }

        public SignalPortsState(Dictionary<string, bool> ports)
        {
            Ports = ports;
        }
    }

    [Serializable, NetSerializable]
    public sealed class SignalPortSelected : BoundUserInterfaceMessage
    {
        public readonly string Port;

        public SignalPortSelected(string port)
        {
            Port = port;
        }
    }
}
