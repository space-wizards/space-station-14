using Content.Shared.DeviceLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Explosion.Components
{
    /// <summary>
    /// Sends a trigger when signal is received.
    /// </summary>
    [RegisterComponent]
    public sealed partial class TriggerOnSignalComponent : Component
    {
        [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>)))]
        public string Port = "Trigger";
    }
}
