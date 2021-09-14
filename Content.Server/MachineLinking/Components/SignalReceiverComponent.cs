using System;
using System.Collections.Generic;
using Content.Server.MachineLinking.Models;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public class SignalReceiverComponent : Component
    {
        public override string Name => "SignalReceiver";

        [DataField("inputs")]
        private List<SignalPort> _inputs = new();

        [ViewVariables]
        public IReadOnlyList<SignalPort> Inputs => _inputs;
    }
}
