using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Chat.Prototypes;

[Prototype("emoteSounds")]
public sealed class EmoteSoundsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("sound")]
    public SoundSpecifier? FallbackSound;

    [DataField("params")]
    public AudioParams? GeneralParams;

    [DataField("sounds", customTypeSerializer: typeof(PrototypeIdDictionarySerializer<SoundSpecifier, EmoteSoundsPrototype>))]
    public Dictionary<string, SoundSpecifier> Sounds = new();
}
