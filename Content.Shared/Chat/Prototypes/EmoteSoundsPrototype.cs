using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.Prototypes;

[Prototype("emoteSounds")]
public sealed class EmoteSoundsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    // todo: custom serializer doesn't support proto id as key
    [DataField("sounds", required: true)]
    public Dictionary<string, SoundSpecifier> Sounds = default!;
}
