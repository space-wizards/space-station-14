using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class SignalReceiverComponent : Component
    {
        [DataField("inputs")]
        private Dictionary<string, List<PortIdentifier>> _inputs = new();

        public void AddPort(string name)
        {
            _inputs.Add(name, new());
        }

        [ViewVariables]
        public IReadOnlyDictionary<string, List<PortIdentifier>> Inputs => _inputs;
    }
}
