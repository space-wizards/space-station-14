using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.Prototypes;

[Prototype("emoteSounds")]
public sealed class EmoteSoundsPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("sounds", required: true)]
    public Dictionary<string, SoundSpecifier> Sounds = default!;
}
