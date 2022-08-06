using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Audio
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class AmbientSoundComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;

        [DataField("sound", required: true), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier Sound = default!;

        /// <summary>
        /// How far away this ambient sound can potentially be heard.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("range")]
        public float Range = 2f;

        /// <summary>
        /// Applies this volume to the sound being played.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("volume")]
        public float Volume = -10f;
    }

    [Serializable, NetSerializable]
    public sealed class AmbientSoundComponentState : ComponentState
    {
        public bool Enabled { get; init; }
        public float Range { get; init; }
        public float Volume { get; init; }
    }
}
