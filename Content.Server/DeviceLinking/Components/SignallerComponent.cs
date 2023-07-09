using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeviceLinking.Components
{
    /// <summary>
    /// Sends out a signal to machine linked objects.
    /// </summary>
    [RegisterComponent]
    public sealed class SignallerComponent : Component
    {
        /// <summary>
        ///     The port that gets signaled when the switch turns on.
        /// </summary>
        [DataField("port", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string Port = "Pressed";
    }
}
