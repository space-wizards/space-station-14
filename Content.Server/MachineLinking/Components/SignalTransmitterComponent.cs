using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    public class SignalTransmitterComponent : Component
    {
        public override string Name => "SignalTransmitter";

        [DataField("outputs")]
        private Dictionary<string, Type> _outputs = new();

        [ViewVariables]
        public IReadOnlyDictionary<string, Type> Outputs => _outputs;
    }
}
