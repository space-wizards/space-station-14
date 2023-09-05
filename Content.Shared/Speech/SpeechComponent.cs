using System.Collections.Specialized;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Speech
{
    /// <summary>
    ///     Component required for entities to be able to speak. (TODO: Entities can speak fine without this, this only forbids them speak if they have it and enabled is false.)
    ///     Contains the option to let entities make noise when speaking, change speech verbs, datafields for the sounds in question, and relevant AudioParams.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SpeechComponent : Component
    {
        [DataField("enabled"), Access(typeof(SpeechSystem),
             Friend = AccessPermissions.ReadWrite,
             Other = AccessPermissions.Read)]
        public bool Enabled = true;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("speechSounds", customTypeSerializer:typeof(PrototypeIdSerializer<SpeechSoundsPrototype>))]
        public string? SpeechSounds;

        /// <summary>
        ///     What speech verb prototype should be used by default for displaying this entity's messages?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("speechVerb", customTypeSerializer:typeof(PrototypeIdSerializer<SpeechVerbPrototype>))]
        public string SpeechVerb = "Default";

        /// <summary>
        ///     A mapping from chat suffixes loc strings to speech verb prototypes that should be conditionally used.
        ///     For things like '?' changing to 'asks' or '!!' making text bold and changing to 'yells'. Can be overridden if necessary.
        /// </summary>
        [DataField("suffixSpeechVerbs", customTypeSerializer:typeof(PrototypeIdValueDictionarySerializer<string, SpeechVerbPrototype>))]
        public Dictionary<string, string> SuffixSpeechVerbs = new()
        {
            { "chat-speech-verb-suffix-exclamation-strong", "DefaultExclamationStrong" },
            { "chat-speech-verb-suffix-exclamation", "DefaultExclamation" },
            { "chat-speech-verb-suffix-question", "DefaultQuestion" },
            { "chat-speech-verb-suffix-stutter", "DefaultStutter" },
            { "chat-speech-verb-suffix-mumble", "DefaultMumble" },
        };

        [DataField("audioParams")]
        public AudioParams AudioParams = AudioParams.Default.WithVolume(6f).WithRolloffFactor(4.5f);

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("soundCooldownTime")]
        public float SoundCooldownTime { get; set; } = 0.5f;

        public TimeSpan LastTimeSoundPlayed = TimeSpan.Zero;
    }
}
