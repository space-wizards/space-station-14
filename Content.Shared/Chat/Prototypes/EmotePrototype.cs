using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.Prototypes;

[Prototype("emote")]
public sealed class EmotePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("category")]
    public EmoteCategory Category = EmoteCategory.General;

    [DataField("words", required: true)]
    public HashSet<string> Words = new();
}

[Flags]
[Serializable, NetSerializable]
public enum EmoteCategory : byte
{
    Invalid = 0,
    Vocal = 1 << 0,
    Hands = 1 << 1,
    Facial = 1 << 2,
    General = byte.MaxValue
}
