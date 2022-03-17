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
    public sealed class SignalTransmitterPort
    {
        [DataField("name", required: true)] public string Name { get; } = default!;

        [DataField("receivers")] public List<(EntityUid entity, string port)> Receivers { get; } = new();

    }

    public static class SignalTransmitterPortExtensions
    {

        public static bool TryGetPort(this IReadOnlyList<SignalTransmitterPort> ports, string name, [NotNullWhen(true)] out SignalTransmitterPort? port)
        {
            return ports.TryFirstOrDefault(port => port.Name == name, out port);
        }

        public static SignalTransmitterPort GetPort(this IReadOnlyList<SignalTransmitterPort> ports, string name)
        {
            if (ports.TryGetPort(name, out var port))
                return port;
            throw new PortNotFoundException();
        }

    }
}
