using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.Exceptions;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.MachineLinking.Models
{
    [DataDefinition]
    public sealed class SignalReceiverPort
    {
        [DataField("name", required: true)] public string Name { get; } = default!;

        public List<SignalTransmitterPort> Transmitters { get; } = new();
    }

    public static class SignalReceiverPortExtensions
    {

        public static bool TryGetPort(this IReadOnlyList<SignalReceiverPort> ports, string name, [NotNullWhen(true)] out SignalReceiverPort? port)
        {
            return ports.TryFirstOrDefault(port => port.Name == name, out port);
        }

        public static SignalReceiverPort GetPort(this IReadOnlyList<SignalReceiverPort> ports, string name)
        {
            if (ports.TryGetPort(name, out var port))
                return port;
            throw new PortNotFoundException();
        }

    }
}
