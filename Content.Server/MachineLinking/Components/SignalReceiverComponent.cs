using System;
using System.Collections.Generic;
using Content.Server.MachineLinking.Models;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class SignalReceiverComponent : Component
    {
        [DataField("inputs")]
        private List<SignalReceiverPort> _inputs = new();

        [ViewVariables]
        public IReadOnlyList<SignalReceiverPort> Inputs => _inputs;
    }
}
