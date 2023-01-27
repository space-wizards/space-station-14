using Content.Server.MachineLinking.System;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    [Access(typeof(SignalLinkerSystem))]
    public sealed partial class SignalReceiverComponent : Component
    {
        [DataField("inputs")]
        public Dictionary<string, List<PortIdentifier>> Inputs = new();
    }
}
