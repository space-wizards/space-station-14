using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    public class SignalReceiverComponent : Component
    {
        public override string Name => "SignalReceiver";

        [DataField("inputs")]
        private Dictionary<string, Type> _inputs = new();

        [ViewVariables]
        public IReadOnlyDictionary<string, Type> Inputs => _inputs;
    }
}
