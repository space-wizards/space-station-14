using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Shared.Speech
{
    /// <summary>
    ///     Component required for entities to be able to speak. (TODO: Entities can speak fine without this, this only forbids them speak if they have it and enabled is false.)
    ///     Contains the option to let entities make noise when speaking, datafields for the sounds in question, and relevant AudioParams.
    /// </summary>
    [RegisterComponent]
    public sealed class SharedSpeechComponent : Component
    {
        [DataField("enabled")]
        private bool _enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("speechSounds", customTypeSerializer:typeof(PrototypeIdSerializer<SpeechSoundsPrototype>))]
        public string? SpeechSounds;

        [DataField("audioParams")]
        public AudioParams AudioParams = AudioParams.Default.WithVolume(6f).WithRolloffFactor(4.5f);

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("soundCooldownTime")]
        public float SoundCooldownTime { get; set; } = 0.5f;

        public TimeSpan LastTimeSoundPlayed = TimeSpan.Zero;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                Dirty();
            }
        }
    }
}
