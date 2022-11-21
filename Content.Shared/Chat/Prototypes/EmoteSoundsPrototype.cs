using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Chat.Prototypes;

[Prototype("emoteSounds")]
public sealed class EmoteSoundsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("params")]
    public AudioParams? Params;

    [DataField("sounds", required: true, customTypeSerializer: typeof(PrototypeIdDictionarySerializer<SoundSpecifier, EmoteSoundsPrototype>))]
    public Dictionary<string, SoundSpecifier> Sounds = default!;
}
