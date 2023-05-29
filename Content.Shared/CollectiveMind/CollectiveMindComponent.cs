using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.CollectiveMind
{
    [RegisterComponent, NetworkedComponent]
    public sealed class CollectiveMindComponent : Component
    {
        [DataField("channel", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<RadioChannelPrototype>))]
        public string Channel = string.Empty;
    }
}
