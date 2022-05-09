using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Robust.Shared.Audio;
using Content.Shared.Sound;

namespace Content.Shared.Speech
{
    [Prototype("speechSounds")]
    public sealed class SpeechSoundsPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataFieldAttribute]
        public string ID { get; } = default!;

        [DataField("saySound")]
        public SoundSpecifier SaySound { get; set; } = new SoundPathSpecifier("/Audio/Voice/Talk/speak_2.ogg");

        [DataField("askSound")]
        public SoundSpecifier AskSound { get; set; } = new SoundPathSpecifier("/Audio/Voice/Talk/speak_2_ask.ogg");

        [DataField("exclaimSound")]
        public SoundSpecifier ExclaimSound { get; set; } = new SoundPathSpecifier("/Audio/Voice/Talk/speak_2_exclaim.ogg");
    }
}
