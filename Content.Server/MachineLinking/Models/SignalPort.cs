using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Server.MachineLinking.Events;
using Content.Server.MachineLinking.Exceptions;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.MachineLinking.Models
{
    [DataDefinition]
    public sealed class SignalPort
    {
        [DataField("name", required: true)] public string Name { get; } = default!;
        [DataField("type")] public Type? Type { get; }
        /// <summary>
        /// Maximum connections of the port. 0 means infinite.
        /// </summary>
        [DataField("maxConnections")] public int MaxConnections { get; } = 0;

        public object? Signal;
    }

    public static class PortPrototypeExtensions{
        public static bool ContainsPort(this IReadOnlyList<SignalPort> ports, string port)
        {
            foreach (var portPrototype in ports)
            {
                if (portPrototype.Name == port)
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<string> GetPortStrings(this IReadOnlyList<SignalPort> ports)
        {
            foreach (var portPrototype in ports)
            {
                yield return portPrototype.Name;
            }
        }

        public static IEnumerable<KeyValuePair<string, bool>> GetValidatedPorts(this IReadOnlyList<SignalPort> ports, Type? validType)
        {
            foreach (var portPrototype in ports)
            {
                yield return new KeyValuePair<string, bool>(portPrototype.Name, portPrototype.Type == validType);
            }
        }

        public static bool TryGetPort(this IReadOnlyList<SignalPort> ports, string name, [NotNullWhen(true)] out SignalPort? port)
        {
            foreach (var portPrototype in ports)
            {
                if (portPrototype.Name == name)
                {
                    port = portPrototype;
                    return true;
                }
            }

            port = null;
            return false;
        }

        public static SignalPort GetPort(this IReadOnlyList<SignalPort> ports, string name)
        {
            foreach (var portPrototype in ports)
            {
                if (portPrototype.Name == name)
                {
                    return portPrototype;
                }
            }

            throw new PortNotFoundException();
        }
    }
}
