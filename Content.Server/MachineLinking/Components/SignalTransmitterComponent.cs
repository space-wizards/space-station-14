using System;
using System.Collections.Generic;
using Content.Server.MachineLinking.Models;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class SignalTransmitterComponent : Component
    {
        [DataField("outputs")]
        private List<SignalPort> _outputs = new();

        [ViewVariables]
        public IReadOnlyList<SignalPort> Outputs => _outputs;
    }
}
