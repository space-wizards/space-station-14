using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech
{
    [Prototype("speechSounds")]
    public readonly record struct SpeechSoundsPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        //Variation is here instead of in SharedSpeechComponent since some sets of
        //sounds may require more fine tuned pitch variation than others.
        [DataField("variation")] public float Variation { get; } = 0.1f;

        [DataField("saySound")]
        public SoundSpecifier SaySound { get; } = new SoundPathSpecifier("/Audio/Voice/Talk/speak_2.ogg");

        [DataField("askSound")]
        public SoundSpecifier AskSound { get; } = new SoundPathSpecifier("/Audio/Voice/Talk/speak_2_ask.ogg");

        [DataField("exclaimSound")]
        public SoundSpecifier ExclaimSound { get; } = new SoundPathSpecifier("/Audio/Voice/Talk/speak_2_exclaim.ogg");
    }
}
