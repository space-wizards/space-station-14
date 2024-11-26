using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech
{
    [Prototype("speechSounds")]
    public sealed partial class SpeechSoundsPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        //Variation is here instead of in SharedSpeechComponent since some sets of
        //sounds may require more fine tuned pitch variation than others.
        [DataField("variation")]
        public float Variation { get; set; } = 0.1f;

        [DataField("saySound")]
        public SoundSpecifier SaySound { get; set; } = new SoundCollectionSpecifier("SpeechSaySound");

        [DataField("askSound")]
        public SoundSpecifier AskSound { get; set; } = new SoundCollectionSpecifier("SpeechAskSound");

        [DataField("exclaimSound")]
        public SoundSpecifier ExclaimSound { get; set; } = new SoundCollectionSpecifier("SpeechExclaimSound");
    }
}
