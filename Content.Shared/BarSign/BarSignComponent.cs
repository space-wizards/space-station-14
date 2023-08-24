using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.BarSign
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class BarSignComponent : Component
    {
        [DataField("current", customTypeSerializer:typeof(PrototypeIdSerializer<BarSignPrototype>))]
        public string? CurrentSign;
    }

    [Serializable, NetSerializable]
    public sealed class BarSignComponentState : ComponentState
    {
        public string? CurrentSign;

        public BarSignComponentState(string? current)
        {
            CurrentSign = current;
        }
    }
}
