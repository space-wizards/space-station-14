using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Chat.Prototypes;

[Prototype("emote")]
public sealed class EmotePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("words", required: true)]
    public HashSet<string> Words = default!;
}
