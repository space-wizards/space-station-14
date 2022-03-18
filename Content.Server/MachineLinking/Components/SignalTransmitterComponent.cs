using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    [DataDefinition]
    public struct PortIdentifier
    {
        [DataField("uid")]
        public EntityUid uid;
        
        [DataField("port")]
        public string port;
    }

    [RegisterComponent]
    public sealed class SignalTransmitterComponent : Component
    {
        [DataField("outputs")]
        private Dictionary<string, List<PortIdentifier>> _outputs = new();

        [ViewVariables]
        public IReadOnlyDictionary<string, List<PortIdentifier>> Outputs => _outputs;
    }
}
