using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Kitchen.Components
{
    [NetworkedComponent]
    public abstract class SharedKitchenSpikeComponent : Component
    {
        [DataField("delay")]
        public float SpikeDelay = 7.0f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("sound")]
        public SoundSpecifier SpikeSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

        [Serializable, NetSerializable]
        public enum KitchenSpikeVisuals : byte
        {
            Status
        }

        [Serializable, NetSerializable]
        public enum KitchenSpikeStatus : byte
        {
            Empty,
            Bloody
        }
    }
}
