using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class TriggerOnSignalReceivedComponent : Component
    {
        [DataField("triggerPort", customTypeSerializer: typeof(PrototypeIdSerializer<ReceiverPortPrototype>))]
        public string TriggerPort = "Trigger";
    }
}
